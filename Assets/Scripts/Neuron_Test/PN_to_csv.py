

# conda activate rdapierre
# start csv_client_v2.py
# start this script
import struct
import sys
import numpy as np
import socket
import argparse
from pythonosc import udp_client


# ----------------------------------------------------------------------------------------
# ----------------------------------------------------------------------------------------
# ----------------------------------------------------------------------------------------
# SETTINGS
recvd_UDP_IP = "127.0.0.1"
recvd_UDP_PORT = 7000
calc = True # indicates data received from calc broadcasting (otherwise receive from csv sending script)
# max_data_signif = 4



# Sending server to max  
send_UDP_IP = "127.0.0.1"
send_UDP_PORT = 8000
data_offset = 15 # calc stream offset (not sure why there are 15 extra values at beginning)
max_data_signif = 4


parser = argparse.ArgumentParser()
parser.add_argument("--ip", default=send_UDP_IP,
    help="The ip of the OSC server")
parser.add_argument("--port", type=int, default=send_UDP_PORT,
    help="The port the OSC server is listening on")
args = parser.parse_args()

client = udp_client.SimpleUDPClient(args.ip, args.port)



# set up socket
recvd_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
recvd_sock.bind((recvd_UDP_IP, recvd_UDP_PORT))


data_rcvd=np.zeros(shape=(1,196))


def __init__(self, host='127.0.0.1', port=7012):
    super().__init__()
    self.host = host
    self.port = port
    self.running = True

    # def run(self):
    # # Create a socket and connect to the server ***why SOCK_STREAM???
    # with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        # s.connect((self.host, self.port))
        
        # while self.running:
            # try:
            # # Receive data from the server
            # data = s.recv(1024)
            # if not data:
                # break
            
            # # Convert the received data (assumed to be in bytes) to a string and split by commas
            # data_str = data.decode('utf-8')
            # data_values = np.array([float(x) for x in data_str.strip().split(',')])
            
            # # Emit the new data to the main thread
            # self.new_data_signal.emit(data_values)
            
            # except Exception as e:
            # print(f"Error receiving data: {e}")
            # break


def recev():
    global data_rcvd

    while True:
        try:
            # Receive data from the server
            data, addr = recvd_sock.recvfrom(2**13)
            

            n = int(len(data)/4)

            data2 = struct.unpack(str(n)+'f', data) # same as little endian float conversion
            #datab=struct.pack(str(n)+'f',*data2)
            #print(f"Taille du paquet reçu : {len(data)} octets")
            buffer=np.array(data2)
            print(buffer.shape)
            data_rcvd=np.vstack((data_rcvd,buffer))
            print(data_rcvd.shape)


            # Emit the new data to the main thread
            #self.new_data_signal.emit(buffer)

                        
        except Exception as e:
            print(f"Error receiving data: {e}")
            



if __name__ == "__main__":

  recev()

np.savetxt('C:\\Users\\pschmidt\\Documents\\CRITAC_pschmidt\\PART-Oriane\\Script\\testNoDisplacement.csv', data_rcvd[1:,:], delimiter=',', fmt='%s')
