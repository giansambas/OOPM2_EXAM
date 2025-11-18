from ultralytics import YOLO
import cv2
import json
import socket
import os
import struct
import time

# ✅ YOLO model path
model_path = r"C:\Users\Gian Sambas\runs\detect\train2\weights\best.pt"
if not os.path.exists(model_path):
    raise FileNotFoundError(f"Model file not found at: {model_path}")

model = YOLO(model_path)

HOST = '127.0.0.1'
PORT = 5051  # Must match C# port

# ✅ Track equipment and durations
equipment_timers = {}
last_seen = {}
INACTIVITY_TIMEOUT = 5  # seconds before stopping timer

def connect_to_server():
    """Keep trying until connected to C# app."""
    while True:
        try:
            s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            s.connect((HOST, PORT))
            print("✅ Connected to C# Gym Tracker!")
            return s
        except Exception:
            print("⏳ Waiting for C# app to start listening...")
            time.sleep(2)

# ✅ Connect to C# first
server = connect_to_server()

cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)
if not cap.isOpened():
    raise RuntimeError("Cannot open webcam")

print("🎥 YOLO gym tracker started — press ESC to quit.")

while True:
    ret, frame = cap.read()
    if not ret:
        break

    results = model(frame, conf=0.25)
    detected_now = set()

    for r in results:
        for box in r.boxes:
            cls = int(box.cls[0])
            conf = float(box.conf[0])
            name = model.names[cls]

            # ✅ Track only gym-related items
            valid_names = [
                "ab roller", "adjustable bench", "assault bike", "barbell", "dumbbell",
                "flat bench", "kettlebell", "medicine ball", "punching bag", "resistance bands",
                "squat rack", "stationary bike", "treadmill", "trx straps", "weight plates", "yoga ball"
            ]

            if name.lower() in valid_names:
                detected_now.add(name)

                # Start or continue timer
                if name not in equipment_timers:
                    equipment_timers[name] = 0
                    last_seen[name] = time.time()
                else:
                    if time.time() - last_seen[name] <= INACTIVITY_TIMEOUT:
                        equipment_timers[name] += 1  # increase duration every frame
                    last_seen[name] = time.time()

    # ✅ Stop tracking equipment not seen for a while
    for eq in list(equipment_timers.keys()):
        if eq not in detected_now and time.time() - last_seen.get(eq, 0) > INACTIVITY_TIMEOUT:
            print(f"⏹ {eq} no longer detected, final duration: {equipment_timers[eq]} sec")
            del equipment_timers[eq]
            del last_seen[eq]

    # ✅ Prepare data for sending
    detections = []
    for name, duration in equipment_timers.items():
        detections.append({
            "label": name,
            "duration": duration
        })

    # ✅ Send JSON safely to C#
    try:
        json_data = json.dumps(detections).encode()
        server.sendall(struct.pack(">I", len(json_data)) + json_data)
    except (ConnectionResetError, ConnectionAbortedError, BrokenPipeError):
        print("⚠️ Lost connection to C# — reconnecting...")
        server.close()
        server = connect_to_server()
        continue

    # ✅ Display live video with detections
    annotated = results[0].plot()
    cv2.imshow("YOLO Gym Detection", annotated)

    if cv2.waitKey(1) & 0xFF == 27:  # ESC key
        break

cap.release()
server.close()
cv2.destroyAllWindows()
