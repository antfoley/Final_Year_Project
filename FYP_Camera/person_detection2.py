import socket
from depthai_sdk import Previews, getDeviceInfo, downloadYTVideo, FPSHandler
from depthai_sdk.managers import PipelineManager, PreviewManager, NNetManager
import depthai as dai
import cv2
import argparse
import blobconverter
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('-nd', '--no-debug', action="store_true", help="Prevent debug output")
parser.add_argument('-cam', '--camera', action="store_true", help="Use DepthAI 4K RGB camera for inference (conflicts with -vid)")
parser.add_argument('-vid', '--video', type=str, help="Path to video file to be used for inference (conflicts with -cam)")
parser.add_argument('-index', nargs='?', type=int, default=0, help="Index of the device to use")
args = parser.parse_args()

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

nm = NNetManager(inputSize=(456,256))
pm = PipelineManager()
pm.setNnManager(nm)

if args.camera:
    fps = FPSHandler()
    pm.createColorCam(previewSize=(456, 256), xout=True)
else:
    cap = cv2.VideoCapture(str(Path(args.video).resolve().absolute()))
    fps = FPSHandler(cap)

blob_path = blobconverter.from_zoo(name='person-detection-retail-0013', shaves=shaves)
nn = nm.createNN(pm.pipeline, pm.nodes, source=Previews.color.name if args.camera else "host", blobPath=Path(blob_path), fullFov=True)
pm.addNn(nn=nn)

def send_data_to_server(data):
    server_ip = '127.0.0.1'
    index = args.index - 1
    server_port = 12345 + index

    print(f'processing index: {index}')

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        sock.sendto(data.encode(), (server_ip, server_port))
    finally:
        sock.close()

with dai.Device(pm.pipeline) as device:

    pv = PreviewManager(display=[Previews.color.name])
    pv.createQueues(device)

    while True:
        if debug:
            pv.prepareFrames()
            pv.showFrames() 


        if cv2.waitKey(1) == ord('q'):
            break