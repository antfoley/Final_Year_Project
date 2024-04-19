import depthai as dai
import threading
import contextlib
import cv2
import blobconverter

# Function to create the pipeline for each camera
def getPipeline():
    pipeline = dai.Pipeline()

    # Create the color camera node
    cam_rgb = pipeline.create(dai.node.ColorCamera)
    cam_rgb.setPreviewSize(456, 256)
    cam_rgb.setBoardSocket(dai.CameraBoardSocket.CAM_A)
    cam_rgb.setResolution(dai.ColorCameraProperties.SensorResolution.THE_1080_P)
    cam_rgb.setInterleaved(False)

    # Create the neural network node
    nn = pipeline.create(dai.node.MobileNetDetectionNetwork)
    nn.setConfidenceThreshold(0.5)
    blob_path = blobconverter.from_zoo(name="person-detection-retail-0013", shaves=6)
    nn.setBlobPath(blob_path)

    # Create output nodes
    xout_rgb = pipeline.create(dai.node.XLinkOut)
    xout_rgb.setStreamName("rgb")
    xout_nn = pipeline.create(dai.node.XLinkOut)
    xout_nn.setStreamName("nn")

    # Linking
    cam_rgb.preview.link(nn.input)
    nn.out.link(xout_nn.input)
    cam_rgb.preview.link(xout_rgb.input)

    return pipeline

# Worker thread function
def worker(dev_info, stack, dic):
    try:
        device = stack.enter_context(dai.Device(dev_info, dai.OpenVINO.Version.VERSION_2021_4, dai.UsbSpeed.HIGH))
        print("=== Connected to " + dev_info.getMxId())

        device.startPipeline(getPipeline())
        dic[dev_info.getMxId()] = {
            "rgb": device.getOutputQueue(name="rgb", maxSize=4, blocking=False),
            "nn": device.getOutputQueue(name="nn", maxSize=4, blocking=False)
        }
    except Exception as e:
        print(f"Failed to initialize device {dev_info.getMxId()}: {str(e)}")

# Main execution
device_infos = dai.Device.getAllAvailableDevices()
print(f'Found {len(device_infos)} devices')

with contextlib.ExitStack() as stack:
    queues = {}
    threads = []
    for dev in device_infos:
        thread = threading.Thread(target=worker, args=(dev, stack, queues))
        thread.start()
        threads.append(thread)

    for t in threads:
        t.join()  # Wait for all threads to finish

    try:
        while True:
            for mxid, device_queues in queues.items():
                rgb_queue = device_queues["rgb"]
                nn_queue = device_queues["nn"]
                if rgb_queue.has() and nn_queue.has():
                    frame = rgb_queue.get().getCvFrame()
                    detections = nn_queue.get().detections
                    height, width = frame.shape[:2]
                    for det in detections:        
                        cv2.rectangle(frame, (int(det.xmin * width), int(det.ymin * height)), (int(det.xmax * width), int(det.ymax * height)), (0, 255, 0), 2)
                        cv2.putText(frame, f"Person: {det.label}", (det.xmin + 10, det.ymin + 20), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 2)
                    cv2.imshow(mxid, frame)
            if cv2.waitKey(1) == ord('q'):
                break
    finally:
        cv2.destroyAllWindows()
        print('Devices closed')
