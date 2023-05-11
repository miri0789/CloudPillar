import os
from azure.iot.hub import IoTHubRegistryManager
from azure.iot.device import Message

connection_string = os.environ["IOTHUB_CONNECTION_STRING"]
device_id = "amanaged01"

iot_hub_registry_manager = IoTHubRegistryManager(connection_string)
import threading

def send_simple_c2d_message(device_id):
    try:
        print(f"Preparing to send simple C2D message to device {device_id}")
        c2d_message = Message("Hello from the backend")
        print("About to call send_c2d_message...")
        
        iot_hub_registry_manager.send_c2d_message(device_id, c2d_message)
        
        print(f"Sent simple C2D message to device {device_id}")
    except Exception as e:
        print(f"Error sending simple C2D message to device {device_id}: {e}")
    except BaseException as e:
        print(f"An unhandled exception occurred: {e}")

def main():
    device_id = "amanaged01"
    send_simple_c2d_message(device_id)
    # t = threading.Thread(target=send_simple_c2d_message, args=(device_id,))
    # t.start()
    # t.join()

if __name__ == "__main__":
    main()
