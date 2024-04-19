import depthai as dai
import cv2
import blobconverter
import numpy as np

# Function to create the pipeline
def getPipeline():
    pipeline = dai.Pipeline()

    # Create the color camera node
    cam_rgb = pipeline.create(dai.node.ColorCamera)
    cam_rgb.setPreviewSize(544, 320)  # Adjust the preview size to match NN requirements
    cam_rgb.setBoardSocket(dai.CameraBoardSocket.CAM_A)
    cam_rgb.setResolution(dai.ColorCameraProperties.SensorResolution.THE_1080_P)
    cam_rgb.setInterleaved(False)
    cam_rgb.setColorOrder(dai.ColorCameraProperties.ColorOrder.BGR)

    # Create stereo depth nodes
    mono_left = pipeline.create(dai.node.MonoCamera)
    mono_right = pipeline.create(dai.node.MonoCamera)
    stereo = pipeline.create(dai.node.StereoDepth)

    mono_left.setResolution(dai.MonoCameraProperties.SensorResolution.THE_400_P)
    mono_left.setBoardSocket(dai.CameraBoardSocket.CAM_B)
    mono_right.setResolution(dai.MonoCameraProperties.SensorResolution.THE_400_P)
    mono_right.setBoardSocket(dai.CameraBoardSocket.CAM_C)

    # Linking mono cameras to stereo depth node
    mono_left.out.link(stereo.left)
    mono_right.out.link(stereo.right)

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
    xout_depth = pipeline.create(dai.node.XLinkOut)
    xout_depth.setStreamName("depth")

    # Linking
    cam_rgb.preview.link(nn.input)
    nn.out.link(xout_nn.input)
    cam_rgb.preview.link(xout_rgb.input)
    stereo.depth.link(xout_depth.input)

    return pipeline

# Main function to execute the pipeline
def main():
    # Create the device with the pipeline
    device = dai.Device(getPipeline())

    # Get output queues
    rgb_queue = device.getOutputQueue(name="rgb", maxSize=4, blocking=False)
    nn_queue = device.getOutputQueue(name="nn", maxSize=4, blocking=False)
    depth_queue = device.getOutputQueue(name="depth", maxSize=4, blocking=False)

    try:
        while True:
            if rgb_queue.has() and nn_queue.has() and depth_queue.has():
                frame = rgb_queue.get().getCvFrame()
                detections = nn_queue.get().detections
                depth_frame = depth_queue.get().getFrame()

                height, width = frame.shape[:2]
                for det in detections:
                    xmin = int(det.xmin * width)
                    ymin = int(det.ymin * height)
                    xmax = int(det.xmax * width)
                    ymax = int(det.ymax * height)
                    cv2.rectangle(frame, (xmin, ymin), (xmax, ymax), (0, 255, 0), 2)
                    depth_value = np.mean(depth_frame[ymin:ymax, xmin:xmax])  # Average depth in the bounding box
                    cv2.putText(frame, f"Person: {det.label} Depth: {depth_value:.2f}mm", (xmin + 10, ymin + 20), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 2)

                cv2.imshow("Detection", frame)
                if cv2.waitKey(1) == ord('q'):
                    break
    finally:
        cv2.destroyAllWindows()
        print("Device closed")

if __name__ == "__main__":
    main()
