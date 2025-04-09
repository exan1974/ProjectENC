# this script works with csv_server_like_calc.py
# offset of 15 digits prior to actual data, encoding/decoding is likely different than calc though

# conda activate oscudp

# start client first, then start server
# kill server with crtl-c fist before killing client

# import struct
import time
import socket
import numpy as np
import pandas as pd
import struct



# SETTINGS ===================================================================================
fncsv = "G:\\BdeB\\Stage\\ProjectENC\\Assets\\Scripts\\Neuron_Test\\Data\\danse_impro_001.csv"
# fncsv = "C:\\Users\\cgatti\\Documents\\CRITAC\\RDADance\\Data\\2024-12-13-UQAM\\trimmed_v1\\Leonie_fente_001_chr01_001.csv"

UDP_IP = "127.0.0.1" # IP address to send data over ("127.0.0.1" if streaming to/from same computer)
# UDP_IP = "10.0.0.192"
UDP_PORT = 7000 # port number
loop = True # toggle to loop sending data or not
delay = 1/100 # time delay for sending each packet (this can be 0)
data_prefix = '\data ' # string to add before numeric data stream (might be needed for Max/MSP to recieve OSC)
# max_data_signif = 4 # number of decimal places

# ============================================================================================
df = pd.read_csv(fncsv, sep=' ',header=None)

NFRAMES = df.shape[0]

IDX = list(range(0,df.shape[1])) # don't take index 0 (frame number in csv file)

n=df.shape[1]

data_names = df.columns.values[IDX].tolist() # data names as list of strings
# print(data_names)

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # create connection
sock.connect((UDP_IP, UDP_PORT))
print('\n >> Streaming CSV data to ' + UDP_IP + '::' + str(UDP_PORT) + ' ...')


# loop data stream
if loop==True:
  while True:    
    for ff in range(NFRAMES):
      # print(ff)
      data2=df.iloc[ff,IDX].tolist()
      #txt = data_prefix + ' '.join(list(map(str, [-1]*15 + df.iloc[ff,IDX].tolist()))) # add 15 numbers to replicate 15 element offset from calc
      datab=struct.pack(str(n)+'f',*data2)
      sock.send(datab)
      time.sleep(delay)
# stream data (no loop)
else:
  for ff in range(NFRAMES):
    # print(ff)
    data2=df.iloc[ff,IDX].tolist()
    #txt = data_prefix + ' '.join(list(map(str, [-1]*15 + df.iloc[ff,IDX].tolist()))) # add 15 numbers to replicate 15 element offset from calc
    datab=struct.pack(str(n)+'f',*data2)
    sock.send(datab)
    time.sleep(delay)

sock.close() # close connection

