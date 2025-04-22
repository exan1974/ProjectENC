#!/usr/bin/env python3
import time
import socket
import pandas as pd
import struct

# SETTINGS ===================================================================================
# Path to your CSV file (update as needed)
# fncsv = "G:\\BdeB\\Stage\\ProjectENC\\Assets\\Scripts\\Neuron_Test\\Data\\Equilibre main\\EquilibreMain_002.csv"
fncsv = "G:\\BdeB\\Stage\\ProjectENC\\Assets\\Scripts\\Neuron_Test\\Data\\Acro\\Sequence_acro_001.csv"
# Destination IP address and UDP port for streaming
UDP_IP = "127.0.0.1"
UDP_PORT = 7000
# Set to True to loop the CSV data until you manually stop the script (e.g., with Ctrl-C)
loop = True
# Time delay between sending packets (in seconds)
delay = 1/100

# ===========================================================================================
# Read the CSV file.
# Note: We no longer use sep=' ' because the file appears to be comma-separated (the default)
try:
    df = pd.read_csv(fncsv, header=None)
except Exception as e:
    print(f"Error reading CSV file: {e}")
    exit(1)

# Optionally, if the CSV’s first column is a frame number or some non-data value, you can skip it:
# df = df.iloc[:, 1:]

# Convert all data to floats to avoid struct.pack errors.
try:
    df = df.astype(float)
except Exception as e:
    print(f"Error converting CSV data to floats: {e}")
    exit(1)

NFRAMES = df.shape[0]
n = df.shape[1]  # number of floats per row

# Create and connect the UDP socket.
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
try:
    sock.connect((UDP_IP, UDP_PORT))
except Exception as e:
    print(f"Error connecting to {UDP_IP}:{UDP_PORT}: {e}")
    exit(1)
print(f"\n>> Streaming CSV data to {UDP_IP}:{UDP_PORT} ...")

# Stream the CSV data.
try:
    if loop:
        while True:
            for ff in range(NFRAMES):
                row_data = df.iloc[ff].tolist()
                try:
                    # Pack the row's floats into binary data. (e.g., if n=10, f"{n}f" becomes "10f")
                    packed_data = struct.pack(f"{n}f", *row_data)
                except struct.error as se:
                    print(f"Error packing data at row {ff}: {se}")
                    continue
                sock.send(packed_data)
                time.sleep(delay)
    else:
        for ff in range(NFRAMES):
            row_data = df.iloc[ff].tolist()
            try:
                packed_data = struct.pack(f"{n}f", *row_data)
            except struct.error as se:
                print(f"Error packing data at row {ff}: {se}")
                continue
            sock.send(packed_data)
            time.sleep(delay)
except KeyboardInterrupt:
    print("\nStreaming interrupted by user.")
finally:
    sock.close()
    print("Socket closed.")
