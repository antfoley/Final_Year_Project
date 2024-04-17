from depthai_sdk import OakCamera, DetectionPacket, Visualizer, TextPosition
import cv2

# Initialize OakCamera for live capture
with OakCamera() as oak:
    color = oak.create_camera('color')
    nn = oak.create_nn('person-detection-retail-0013', color)

    def cb(packet: DetectionPacket, visualizer: Visualizer):
        num = len(packet.img_detections.detections)
        print('New msgs! Number of people detected:', num)

        visualizer.add_text(f"Number of people: {num}", position=TextPosition.TOP_MID)
        visualizer.draw(packet.frame)
        cv2.imshow(f'frame {packet.name}', packet.frame)
        if cv2.waitKey(1) & 0xFF == ord('q'):  # Add a way to quit the display
            return False  # Returning False stops the loop

    oak.visualize(nn, callback=cb)
    oak.start(blocking=True)
