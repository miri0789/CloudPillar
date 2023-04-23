using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Devices.Shared;

namespace FirmwareUpdateAgent
{
    public class TwinAction
    {
        private const string _ChangeSpecKey = "changeSpec";
        private readonly string _changeSpec, _recipe, _stage, _path;
        private readonly int _step;
        private readonly DeviceClient _deviceClient;
        private readonly JObject _desired;
        private readonly JObject _reported;

        // Constructor
        public TwinAction(DeviceClient deviceClient, JObject desired, JObject reported, string changeSpec, string recipe, string stage, int step, string path)
        {
            _deviceClient = deviceClient;
            _desired = desired;
            _reported = reported;
            _changeSpec = changeSpec;
            _recipe = recipe;
            _stage = stage;
            _step = step;
            _path = path;
        }

        // Get the desired properties of the TwinAction
        public JObject Desired { get { return (_desired[_changeSpec]![_recipe]![_stage]![_step]! as JObject)!; } }

        // Get the reported properties of the TwinAction
        public JObject? Reported {
            get {
                try {
                    return _reported[_changeSpec]?[_recipe]?[_stage]?[_step] as JObject;
                } catch(Exception x) {
                    return null;
                }
            }
        }

        // Ensure that the reported object contains the required keys
        private void EnsureReported()
        {
            if (!_reported.ContainsKey(_changeSpec))
            {
                _reported[_changeSpec] = JObject.Parse("{}");
            }
            if (!(_reported[_changeSpec]! as JObject)!.ContainsKey(_recipe))
            {
                _reported[_changeSpec]![_recipe] = JObject.Parse("{}");
            }
            if (!(_reported[_changeSpec]![_recipe] as JObject)!.ContainsKey(_stage))
            {
                _reported[_changeSpec]![_recipe]![_stage] = JContainer.Parse("[]");
            }

            JContainer stageObject = (_reported[_changeSpec]![_recipe]![_stage] as JContainer)!;
            for (int i = stageObject.Count; i < _step + 1; i++)
                stageObject!.Add(JObject.Parse("{}"));
        }

        // Set the action status to Pending, InProgress, Failed, or Complete
        public void ReportPending() { Status = "Pending"; }
        public void ReportProgress(int percent = 0) { Status = "InProgress"; Reported!["Progress"] = percent; }
        public void ReportFailed(string ResultCode, string ResultText) { Status = "Failed"; Reported!["ResultCode"] = ResultCode; Reported!["ResultText"] = ResultText; }
        public void ReportSuccess(string ResultCode, string ResultText) { Status = "Success"; Reported!["ResultCode"] = ResultCode; Reported!["ResultText"] = ResultText; Reported!["Progress"]?.Remove(); }

        // Get or set the action status
        public String? Status
        {
            get { return Reported?["status"]?.ToString(); }
            set { EnsureReported(); Reported!["status"] = value; }
        }

        // Check the current status of the action
        public bool IsPending { get { return "Pending" == Status; } }
        public bool IsInProgress { get { return "InProgress" == Status; } }
        public bool IsFailed { get { return "Failed" == Status; } }
        public bool IsSuccess { get { return "Success" == Status; } }
        public bool IsComplete { get { return IsFailed || IsSuccess;} }

        // Persist the action status to the device twin
        public async Task Persist() {
            await UpdateReportedPropertiesAsync(_deviceClient, _ChangeSpecKey, _reported[_ChangeSpecKey] as JObject);
        }

        // Helper method to update the reported properties of the device twin
        private static async Task UpdateReportedPropertiesAsync(DeviceClient deviceClient, string key, object value)
        {
            var updatedReportedProperties = new TwinCollection();
            updatedReportedProperties[key] = value;
            await deviceClient.UpdateReportedPropertiesAsync(updatedReportedProperties);
        }

        // Main method to report the twin state and process the actions
        public static async Task<List<TwinAction>> ReportTwinState(CancellationToken cancellationToken, DeviceClient deviceClient, string deviceState, Func<CancellationToken, TwinAction, Task> processor = null)
        {   
            var actions = new List<TwinAction>();
            try {
                var currentTwin = await deviceClient.GetTwinAsync();

                var desiredJObject = JObject.Parse(currentTwin.Properties.Desired.ToJson());
                var reportedJObject = JObject.Parse(currentTwin.Properties.Reported.ToJson());

                reportedJObject["deviceState"] = deviceState;
                await UpdateReportedPropertiesAsync(deviceClient, "deviceState", deviceState);

                JObject changeSpecJObject = (JObject)desiredJObject[_ChangeSpecKey]!;

                if (!JObject.DeepEquals(changeSpecJObject["id"], reportedJObject[_ChangeSpecKey]?["id"]))
                {
                    reportedJObject[_ChangeSpecKey] = JObject.Parse("{\"id\": \"" + changeSpecJObject["id"] + "\"}");
                }

                // Loop through the recipes, stages, and steps in the changeSpec
                foreach (var recipe in changeSpecJObject)
                {
                    JObject? recipeJObject = recipe.Value as JObject;
                    if (recipeJObject == null) continue;
                    var nextRecipe = recipeJObject.Parent!.Next;
                    bool goingFallback = false;
                    foreach (var stage in recipeJObject)
                    {
                        if(goingFallback) {
                            break; // Go to next recipe
                        }
                        JContainer? stageJContainer = stage.Value as JContainer;
                        if (stageJContainer == null) continue;
                        for (int i = 0; i < stageJContainer.Count; i++)
                        {
                            var step = stageJContainer[i]!;
                            var path = step.Path;
                            var action = new TwinAction(deviceClient, desiredJObject, reportedJObject, _ChangeSpecKey, recipe.Key, stage.Key, i, path);
                            if(action.Status == null || action.IsPending || action.IsInProgress)
                                actions.Add(action);
                            else
                            if(action.IsFailed) {
                                // Handle failed actions and fallback to next recipe
                                reportedJObject[_ChangeSpecKey]!["Status"] = nextRecipe != null ? $"Falling back to next recipe '{(nextRecipe as JProperty).Name}'" : "Failed";
                                reportedJObject[_ChangeSpecKey]!["lastFaultedRecipe"] = recipe.Key;
                                reportedJObject[_ChangeSpecKey]!["lastFaultedPath"] = path;
                                await action.Persist();
                                if(nextRecipe == null) // Fatal error, no fallback
                                    return actions;
                                else
                                    goingFallback = true;
                            }
                        }
                    }

                    // Check if fallback is needed or the recipe is successful
                    if(goingFallback && nextRecipe != null) {
                        actions.Clear();
                    } else {
                        if (actions.Count == 0) { // Empty list and no fallback means Success
                            reportedJObject[_ChangeSpecKey]!["Status"] = "Complete";
                        }
                        break;
                    }
                }
                
                // Set the status of actions and update the reported properties
                foreach(var action in actions) {
                    if(cancellationToken.IsCancellationRequested)
                        break;
                    if(!action.IsInProgress) // Leave InProgress intact
                        action.ReportPending();
                }
                await UpdateReportedPropertiesAsync(deviceClient, _ChangeSpecKey, reportedJObject[_ChangeSpecKey] as JObject);

                // If a processor is provided, process the actions
                if (processor != null) {
                    foreach(var action in actions) {
                        if(cancellationToken.IsCancellationRequested)
                            break;
                        reportedJObject[_ChangeSpecKey]!["actionPath"] = action._path;
                        reportedJObject[_ChangeSpecKey]!["Status"] = "InProgress";
                        await action.Persist();
                        try {
                            await processor(cancellationToken, action);
                        } catch (Exception x) {
                            action.ReportFailed(x.GetType().Name, x.Message);
                        }
                        await action.Persist();
                        if(action.IsFailed) 
                            break;
                    }
                }
            } catch (Exception x) {
                Console.WriteLine("{0}: Exception hit when analysing the twin: {1}", DateTime.Now, x.Message);
            }
            return actions;
        }
    };
}
