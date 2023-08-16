#!/bin/bash

import socket
import sys
import time

if len(sys.argv) < 3:
    print("USAGE:\n\tserver.py <SERVER_IP> <PORT_NO>")
    sys.exit(1)
HOST = sys.argv[1]
PORTNO = int(sys.argv[2])

print("opening socket connection...")
sk = socket.socket( socket.AF_INET, socket.SOCK_STREAM )
print( f"trying to open server using \n\tHOST {HOST} \n\tPORTNO {PORTNO}" )
sk.bind( (HOST, PORTNO) )
sk.listen( 5 )
time.sleep(1)
print("OK - listening")

print( "waiting connection..." )
new_socket, new_socket_addr = sk.accept( )
print( "Conenction received! sleeping 5s..." )
time.sleep(5)

new_socket.close( )
sk.close( )
print("closing...")