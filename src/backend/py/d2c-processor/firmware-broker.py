import os
import json
import asyncio
import time
from azure.iot.hub import IoTHubRegistryManager
from azure.iot.hub.models import CloudToDeviceMethod
from azure.storage.blob import BlobServiceClient, BlobClient
from azure.eventhub.aio import EventHubConsumerClient
from azure.eventhub.extensions.checkpointstoreblobaio import BlobCheckpointStore
from azure.iot.device import Message

connection_string = os.environ["IOTHUB_CONNECTION_STRING"]
event_hub_compatible_endpoint = os.environ["IOTHUB_EVENT_HUB_COMPATIBLE_ENDPOINT"]
event_hub_compatible_path = os.environ["IOTHUB_EVENT_HUB_COMPATIBLE_PATH"]
storage_connection_string = os.environ["STORAGE_CONNECTION_STRING"]
container_name = os.environ["BLOB_CONTAINER_NAME"]
partition_id = os.environ.get("D2C_PARTITION_ID")  # None if the environment variable is not set

iot_hub_registry_manager = IoTHubRegistryManager(connection_string)
blob_service_client = BlobServiceClient.from_connection_string(storage_connection_string)
container_client = blob_service_client.get_container_client(container_name)

async def on_event(partition_context, event):
    await process_event(event)
    await partition_context.update_checkpoint(event)

async def receive_device_to_cloud_messages(partition_id=None):
    checkpoint_store = BlobCheckpointStore.from_connection_string(storage_connection_string, container_name)

    event_hub_consumer_client = EventHubConsumerClient.from_connection_string(
        event_hub_compatible_endpoint,
        consumer_group="$Default",
        eventhub_path=event_hub_compatible_path,
        checkpoint_store=checkpoint_store,
    )

    async with event_hub_consumer_client:
        if partition_id is not None:
            await event_hub_consumer_client.receive(on_event, partition_id=partition_id)
        else:
            await event_hub_consumer_client.receive(on_event)

async def process_event(event):
    event_data = json.loads(event.body_as_str())
    if event_data.get("event_type") == "FirmwareUpdateReady":
        device_id = event.properties[b"device_id"].decode("utf-8")
        filename = event_data["filename"]
        chunk_size = event_data["chunk_size"]
        await send_firmware_update_async(device_id, filename, chunk_size)

async def send_firmware_update_async(device_id, filename, chunk_size):
    try:
        blob_client = container_client.get_blob_client(filename)
        blob_size = blob_client.get_blob_properties().size
        total_chunks = (blob_size + chunk_size - 1) // chunk_size

        for chunk_index in range(total_chunks):
            offset = chunk_index * chunk_size
            data = blob_client.download_blob(offset, chunk_size).readall()
            message_payload = {
                "filename": filename,
                "chunk_index": chunk_index,
                "write_position": offset,
                "total_chunks": total_chunks,
                "data": data.hex(),
            }
            c2d_message_json = json.dumps(message_payload)
            c2d_message = Message(c2d_message_json)
            c2d_message.message_id = f"{filename}_{chunk_index}"
            message_size = len(c2d_message_json)
            print(f"Message size: {message_size} bytes")
            c2d_message.custom_properties["chunk_index"] = str(chunk_index)
            c2d_message.custom_properties["total_chunks"] = str(total_chunks)
            print(f"Sending C2D message to device {device_id}: {c2d_message_json}")
            iot_hub_registry_manager.send_c2d_message(device_id, c2d_message)
            print(f"Sent C2D message to device {device_id}")
            await asyncio.sleep(10)  # Sleep for 10 seconds
    except Exception as e:
        print(f"Error sending C2D message to device {device_id}: {e}")

if __name__ == "__main__":
    while True:
        try:
            asyncio.run(receive_device_to_cloud_messages(partition_id))
        except Exception as e:
            print(f"Error in receive_device_to_cloud_messages: {e}")
        time.sleep(5)  # Sleep for a short duration before restarting
