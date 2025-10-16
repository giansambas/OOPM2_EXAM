from ultralytics import YOLO
import cv2
import json
import socket
import os
import struct

model_path = r"C:\Users\Gian Sambas\runs\detect\train2\weights\best.pt"
if not os.path.exists(model_path):
    raise FileNotFoundError(f"Model file not found at: {model_path}")

model = YOLO(model_path)

HOST = '127.0.0.1'
PORT = 5050  

server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server.connect((HOST, PORT))

cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)
if not cap.isOpened():
    raise RuntimeError("Cannot open webcam")

print("✅ Gym equipment detection started. Press ESC to quit.")

while True:
    ret, frame = cap.read()
    if not ret:
        break

    results = model(frame, conf=0.25)
    detections = []

    for r in results:
        for box in r.boxes:
            cls = int(box.cls[0])
            conf = float(box.conf[0])
            name = model.names[cls]

            if name.lower() in [ "ab roller", "adjustable bench", "assault bike", "barbell", "dumbbell",
    "flat bench", "kettlebell", "medicine ball", "punching bag", "resistance bands", "squat rack", "stationary bike", "treadmill", "trx straps", "weight plates", "yoga ball" ]:
                x1, y1, x2, y2 = box.xyxy[0].tolist()
                detections.append({
                    "label": name,
                    "conf": round(conf, 2),
                    "bbox": [x1, y1, x2, y2]
                })

    json_data = json.dumps(detections).encode()
    server.sendall(struct.pack(">I", len(json_data)) + json_data)

    annotated = results[0].plot()
    cv2.imshow("YOLO Gym Detection", annotated)

    if cv2.waitKey(1) & 0xFF == 27:
        break

cap.release()
server.close()
cv2.destroyAllWindows()
