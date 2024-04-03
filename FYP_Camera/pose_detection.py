import argparse
import threading
from pathlib import Path
import socket
import sys

from depthai_sdk.managers import PipelineManager, NNetManager, BlobManager, PreviewManager
from depthai_sdk import FPSHandler, Previews, getDeviceInfo, downloadYTVideo
import blobconverter

from pose import getKeypoints, getValidPairs, getPersonwiseKeypoints, keypointsMapping
import cv2
import depthai as dai
import numpy as np

parser = argparse.ArgumentParser()
parser.add_argument('-nd', '--no-debug', action="store_true", help="Prevent debug output")
parser.add_argument('-cam', '--camera', action="store_true", help="Use DepthAI 4K RGB camera for inference (conflicts with -vid)")
parser.add_argument('-vid', '--video', type=str, help="Path to video file to be used for inference (conflicts with -cam)")
parser.add_argument('-index', nargs='?', type=int, default=0, help="Index of the device to use")
args = parser.parse_args()

np.set_printoptions(threshold=sys.maxsize)

if not args.camera and not args.video:
    raise RuntimeError("No source selected. Please use either \"-cam\" to use RGB camera as a source or \"-vid <path>\" to run on video")
if not (int(args.index)+1):
    raise RuntimeError("Please specify the index of the device to use. Use \"-index <index>\"")

debug = not args.no_debug
device_info = getDeviceInfo()

if args.camera:
    shaves = 6
else:
    shaves = 8
    if str(args.video).startswith('https'):
        args.video = downloadYTVideo(str(args.video))
        print("Youtube video downloaded.")
    if not Path(args.video).exists():
        raise ValueError("Path {} does not exists!".format(args.video))

blob_path = blobconverter.from_zoo(name="human-pose-estimation-0001", shaves=shaves)

colors = [[0, 100, 255], [0, 100, 255], [0, 255, 255], [0, 100, 255], [0, 255, 255], [0, 100, 255], [0, 255, 0],
          [255, 200, 100], [255, 0, 255], [0, 255, 0], [255, 200, 100], [255, 0, 255], [0, 0, 255], [255, 0, 0],
          [200, 200, 0], [255, 0, 0], [200, 200, 0], [0, 0, 0]]
POSE_PAIRS = [[1, 2], [1, 5], [2, 3], [3, 4], [5, 6], [6, 7], [1, 8], [8, 9], [9, 10], [1, 11], [11, 12], [12, 13],
              [1, 0], [0, 14], [14, 16], [0, 15], [15, 17], [2, 17], [5, 16]]
running = True  # Flag to control the main loop
pose = None  # Placeholder for pose data
keypoints_list = None  # Placeholder for keypoints list
detected_keypoints = None  # Placeholder for detected keypoints
personwiseKeypoints = None  # Placeholder for person-wise keypoints

nm = NNetManager(inputSize=(456, 256))  # Create an instance of NNetManager with input size (456, 256)
pm = PipelineManager()  # Create an instance of PipelineManager
pm.setNnManager(nm)  # Set the NNetManager for the PipelineManager

if args.camera:
    fps = FPSHandler()  # Create an instance of FPSHandler for camera mode
    pm.createColorCam(previewSize=(456, 256), xout=True)  # Create a color camera pipeline with preview size (456, 256) and xout enabled
else:
    cap = cv2.VideoCapture(str(Path(args.video).resolve().absolute()))  # Open the video file specified in args.video
    fps = FPSHandler(cap)  # Create an instance of FPSHandler for video mode

nn = nm.createNN(pm.pipeline, pm.nodes, source=Previews.color.name if args.camera else "host", blobPath=Path(blob_path), fullFov=True)  # Create a neural network node with the specified parameters
pm.addNn(nn=nn)  # Add the neural network node to the pipeline


# Create nodes
# monoLeft = pm.pipeline.createMonoCamera()
# monoRight = pm.pipeline.createMonoCamera()
# stereo = pm.pipeline.createStereoDepth()

# # Configure mono cameras
# monoLeft.setBoardSocket(dai.CameraBoardSocket.LEFT)
# monoRight.setBoardSocket(dai.CameraBoardSocket.RIGHT)
# monoLeft.setResolution(dai.MonoCameraProperties.SensorResolution.THE_400_P)
# monoRight.setResolution(dai.MonoCameraProperties.SensorResolution.THE_400_P)

# # Configure stereo depth
# stereo.setConfidenceThreshold(255)
# stereo.setDepthAlign(dai.CameraBoardSocket.RGB)
# stereo.setOutputDepth(True)
# stereo.setOutputRectified(True)
# stereo.setRectifyEdgeFillColor(0) # Black, to better see the cutout
# stereo.setRectifyMirrorFrame(False)

# # Linking
# monoLeft.out.link(stereo.left)
# monoRight.out.link(stereo.right)

# # Create output
# xoutDepth = pm.pipeline.createXLinkOut()
# xoutDepth.setStreamName("depth")
# stereo.depth.link(xoutDepth.input)

def decode_thread(in_queue):
    global keypoints_list, detected_keypoints, personwiseKeypoints

    while running:
        try:
            raw_in = in_queue.get()  # Get the next input from the input queue
        except RuntimeError:
            return  # Exit the thread if there is a runtime error
        fps.tick('nn')  # Update the FPS counter for the neural network processing
        heatmaps = np.array(raw_in.getLayerFp16('Mconv7_stage2_L2')).reshape((1, 19, 32, 57))  # Get the heatmaps from the neural network output
        pafs = np.array(raw_in.getLayerFp16('Mconv7_stage2_L1')).reshape((1, 38, 32, 57))  # Get the Part Affinity Fields (PAFs) from the neural network output
        heatmaps = heatmaps.astype('float32')  # Convert the heatmaps to float32 data type
        pafs = pafs.astype('float32')  # Convert the PAFs to float32 data type
        outputs = np.concatenate((heatmaps, pafs), axis=1)  # Concatenate the heatmaps and PAFs along the channel axis

        new_keypoints = []  # Placeholder for new keypoints
        new_keypoints_list = np.zeros((0, 3))  # Placeholder for new keypoints list
        keypoint_id = 0  # Counter for keypoint IDs

        for row in range(18): # Iterate through the rows of the outputs, each index is pose
            probMap = outputs[0, row, :, :]  # Get the probability map for the current keypoint
            probMap = cv2.resize(probMap, nm.inputSize)  # Resize the probability map to the input size of the neural network
            keypoints = getKeypoints(probMap, 0.3)  # Get the keypoints from the probability map with a threshold of 0.3
            new_keypoints_list = np.vstack([new_keypoints_list, *keypoints])  # Add the keypoints to the keypoints list
            keypoints_with_id = []

            for i in range(len(keypoints)):
                keypoints_with_id.append(keypoints[i] + (keypoint_id,))  # Add the keypoint ID to each keypoint
                keypoint_id += 1

            new_keypoints.append(keypoints_with_id)  # Add the keypoints with IDs to the new keypoints list

        valid_pairs, invalid_pairs = getValidPairs(outputs, w=nm.inputSize[0], h=nm.inputSize[1], detected_keypoints=new_keypoints)  # Get the valid and invalid pairs of keypoints
        newPersonwiseKeypoints = getPersonwiseKeypoints(valid_pairs, invalid_pairs, new_keypoints_list)  # Get the person-wise keypoints

        detected_keypoints, keypoints_list, personwiseKeypoints = (new_keypoints, new_keypoints_list, newPersonwiseKeypoints)  # Update the detected keypoints, keypoints list, and person-wise keypoints

def send_data_to_server(data):
    server_ip = '127.0.0.1'
    server_port = 12345

    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.connect((server_ip, server_port))
        sock.sendall(data.encode())

        # Waiting for ACK from the server
        ack = sock.recv(1024)
        print(f"Server acknowledged with: {ack.decode()}")


global A, B
A = None
B = None
def show(frame):
    global keypoints_list, detected_keypoints, personwiseKeypoints, nm, A, B, indexOfPose

    if keypoints_list is not None and detected_keypoints is not None and personwiseKeypoints is not None:
        scale_factor = frame.shape[0] / nm.inputSize[1]  # Calculate the scale factor for resizing the keypoints
        offset_w = int(frame.shape[1] - nm.inputSize[0] * scale_factor) // 2  # Calculate the offset for aligning the keypoints

        def scale(point):
            return int(point[0] * scale_factor) + offset_w, int(point[1] * scale_factor)  # Scale the point coordinates

        for i in range(18):
            if len(detected_keypoints[i]) > 0:
                for j in range(len(detected_keypoints[i])):
                    try:
                        cv2.circle(frame, scale(detected_keypoints[i][j][0:2]), 5, colors[i], -1, cv2.LINE_AA)  # Draw circles at the detected keypoints
                    except:
                        pass
        for i in range(17):
            for n in range(len(personwiseKeypoints)):
                try:
                    index = personwiseKeypoints[n][np.array(POSE_PAIRS[i])]  # Get the indices of the keypoints for the current person and pose pair
                except:
                    continue
                if -1 not in index:
                    try:
                        B = np.int32(keypoints_list[index.astype(int), 0])  # Get the x-coordinates of the keypoints
                        A = np.int32(keypoints_list[index.astype(int), 1])  # Get the y-coordinates of the keypoints
                        indexOfPose = i
                        #keypointsMapping = ['Nose'0, 'Neck'1, 'R-Sho'2, 'R-Elb'3, 'R-Wr'4, 'L-Sho'5, 'L-Elb'6, 'L-Wr'7, 'R-Hip'8, 'R-Knee'9, 'R-Ank'10,
                        #            'L-Hip'11, 'L-Knee'12, 'L-Ank'13, 'R-Eye'14, 'L-Eye'15, 'R-Ear'16, 'L-Ear'17]
                        #notIncludedData = [1, 3, 4, 5, 6, 8, 9, 11, 12, 14, 15, 16, 17] #not including anything about neck
                        includedData = [0] #including only the body parts that are needed
                        if indexOfPose in includedData:
                            # with np.printoptions(threshold=np.inf):
                            #     with open('coordinates.txt', 'a') as f:  # Use 'a' mode to append to the file instead of overwriting it
                            #         f.write(f"Body Part: {keypointsMapping[indexOfPose]}, Starting Point: ({B[0]},{A[0]}), Ending Point: ({B[1]},{A[1]}) \n")  # Append the coordinates to the file 
                            send_data_to_server(f"Body_Part: {keypointsMapping[indexOfPose]} Starting_Point: ({B[0]},{A[0]}) Camera_Index: {int(args.index)}\n")
                            #print(f"Body_Part: {keypointsMapping[indexOfPose]} Starting_Point: ({B[0]},{A[0]}) Ending_Point: ({B[1]},{A[1]}) \n")
                        cv2.line(frame, scale((B[0], A[0])), scale((B[1], A[1])), colors[i], 3, cv2.LINE_AA)  # Draw lines connecting the keypoints
                    except:
                        pass

# Starting pipeline...
print("Starting pipeline...")
with dai.Device(pm.pipeline, device_info) as device:
    if args.camera:
        pv = PreviewManager(display=[Previews.color.name], nnSource=Previews.color.name, fpsHandler=fps)  # Create an instance of PreviewManager for camera mode
        pv.createQueues(device)  # Create queues for the preview frames
    nm.createQueues(device)  # Create queues for the neural network input and output
    seq_num = 1  # Sequence number for frames
    
    t1 = threading.Thread(target=decode_thread, args=(nm.outputQueue, ))  # Create a thread for decoding the neural network output
    t1.start()  # Start the decoding thread

    # t2 = threading.Thread(target=pipe_forward_kinematics, args=(personwiseKeypoints, ))
    # t2.start()

    def should_run():
        return cap.isOpened() if args.video else True  # Check if the video capture is open or if it's camera mode

    try:
        while should_run():
            PosePart = None
            # inDepth = qDepth.tryGet()  # Retrieve depth map
            # if inDepth is not None:
            #     depthFrame = inDepth.getFrame()  # Get the depth frame
            fps.nextIter()  # Update the FPS counter
            # if indexOfPose is not None:
            #     PosePart = keypointsMapping[indexOfPose]  # Define the PosePart variable

            if args.camera:
                pv.prepareFrames()  # Prepare the frames for preview
                frame = pv.get(Previews.color.name)  # Get the color frame from the preview
                # depthFrame = (xoutDepth.getStreamName())  # Get the depth frame from the preview
                if debug:
                    show(frame)  # Show the keypoints on the frame
                    cv2.putText(frame, f"RGB FPS: {round(fps.tickFps(Previews.color.name), 1)}", (5, 15), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0))  # Display the RGB FPS
                    cv2.putText(frame, f"NN FPS:  {round(fps.tickFps('nn'), 1)}", (5, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0))  # Display the NN FPS
                    pv.showFrames()  # Show the preview frames
                    # if PosePart is not None and A is not None and B is not None and depthFrame is not None:            
                    #     print(f"Depth of {PosePart} is {depthFrame.getCvFrame[A.astype(int),B.astype(int)]} mm")
            if not args.camera:
                read_correctly, frame = cap.read()  # Read the next frame from the video capture

                if not read_correctly:
                    break  # Break the loop if the frame is not read correctly

                nm.sendInputFrame(frame)  # Send the frame to the neural network input
                fps.tick('host')  # Update the FPS counter for the host processing

                if debug:
                    show(frame)  # Show the keypoints on the frame
                    cv2.putText(frame, f"RGB FPS: {round(fps.tickFps('host'), 1)}", (5, 15), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0))  # Display the RGB FPS
                    cv2.putText(frame, f"NN FPS:  {round(fps.tickFps('nn'), 1)}", (5, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0))  # Display the NN FPS
                    cv2.imshow("rgb", frame)  # Show the RGB frame
            key = cv2.waitKey(1)  # Wait for a key press
            if key == ord('q'):
                break  # Break the loop if the 'q' key is pressed

    except KeyboardInterrupt:
        pass

    running = False  # Set the running flag to False to stop the decoding thread

t1.join()  # Wait for the decoding thread to finish
# t2.join()
fps.printStatus()  # Print the final FPS status
if not args.camera:
    cap.release()  # Release the video capture
