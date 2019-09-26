#!/bin/sh

VERSION="v2.0.7"

###########################################
#---------------) Colors (----------------#
###########################################

C=$(printf '\033')
RED="${C}[1;31m"
GREEN="${C}[1;32m"
Y="${C}[1;33m"
B="${C}[1;34m"
LG="${C}[1;37m" #LightGray
DG="${C}[1;90m" #DarkGray
NC="${C}[0m"


###########################################
#---------------) Lists (-----------------#
###########################################

filename="linpeas.txt"
kernelB=" 3.9.6\| 3.9.0\| 3.9\| 3.8.9\| 3.8.8\| 3.8.7\| 3.8.6\| 3.8.5\| 3.8.4\| 3.8.3\| 3.8.2\| 3.8.1\| 3.8.0\| 3.8\| 3.7.6\| 3.7.0\| 3.7\| 3.6.0\| 3.6\| 3.5.0\| 3.5\| 3.4.9\| 3.4.8\| 3.4.6\| 3.4.5\| 3.4.4\| 3.4.3\| 3.4.2\| 3.4.1\| 3.4.0\| 3.4\| 3.3\| 3.2\| 3.19.0\| 3.16.0\| 3.15\| 3.14\| 3.13.1\| 3.13.0\| 3.13\| 3.12.0\| 3.12\| 3.11.0\| 3.11\| 3.10.6\| 3.10.0\| 3.10\| 3.1.0\| 3.0.6\| 3.0.5\| 3.0.4\| 3.0.3\| 3.0.2\| 3.0.1\| 3.0.0\| 2.6.9\| 2.6.8\| 2.6.7\| 2.6.6\| 2.6.5\| 2.6.4\| 2.6.39\| 2.6.38\| 2.6.37\| 2.6.36\| 2.6.35\| 2.6.34\| 2.6.33\| 2.6.32\| 2.6.31\| 2.6.30\| 2.6.3\| 2.6.29\| 2.6.28\| 2.6.27\| 2.6.26\| 2.6.25\| 2.6.24.1\| 2.6.24\| 2.6.23\| 2.6.22\| 2.6.21\| 2.6.20\| 2.6.2\| 2.6.19\| 2.6.18\| 2.6.17\| 2.6.16\| 2.6.15\| 2.6.14\| 2.6.13\| 2.6.12\| 2.6.11\| 2.6.10\| 2.6.1\| 2.6.0\| 2.4.9\| 2.4.8\| 2.4.7\| 2.4.6\| 2.4.5\| 2.4.4\| 2.4.37\| 2.4.36\| 2.4.35\| 2.4.34\| 2.4.33\| 2.4.32\| 2.4.31\| 2.4.30\| 2.4.29\| 2.4.28\| 2.4.27\| 2.4.26\| 2.4.25\| 2.4.24\| 2.4.23\| 2.4.22\| 2.4.21\| 2.4.20\| 2.4.19\| 2.4.18\| 2.4.17\| 2.4.16\| 2.4.15\| 2.4.14\| 2.4.13\| 2.4.12\| 2.4.11\| 2.4.10\| 2.2.24"
kernelDCW_Ubuntu_Precise_1="3.1.1-1400-linaro-lt-mx5\|3.11.0-13-generic\|3.11.0-14-generic\|3.11.0-15-generic\|3.11.0-17-generic\|3.11.0-18-generic\|3.11.0-20-generic\|3.11.0-22-generic\|3.11.0-23-generic\|3.11.0-24-generic\|3.11.0-26-generic\|3.13.0-100-generic\|3.13.0-24-generic\|3.13.0-27-generic\|3.13.0-29-generic\|3.13.0-30-generic\|3.13.0-32-generic\|3.13.0-33-generic\|3.13.0-34-generic\|3.13.0-35-generic\|3.13.0-36-generic\|3.13.0-37-generic\|3.13.0-39-generic\|3.13.0-40-generic\|3.13.0-41-generic\|3.13.0-43-generic\|3.13.0-44-generic\|3.13.0-46-generic\|3.13.0-48-generic\|3.13.0-49-generic\|3.13.0-51-generic\|3.13.0-52-generic\|3.13.0-53-generic\|3.13.0-54-generic\|3.13.0-55-generic\|3.13.0-57-generic\|3.13.0-58-generic\|3.13.0-59-generic\|3.13.0-61-generic\|3.13.0-62-generic\|3.13.0-63-generic\|3.13.0-65-generic\|3.13.0-66-generic\|3.13.0-67-generic\|3.13.0-68-generic\|3.13.0-71-generic\|3.13.0-73-generic\|3.13.0-74-generic\|3.13.0-76-generic\|3.13.0-77-generic\|3.13.0-79-generic\|3.13.0-83-generic\|3.13.0-85-generic\|3.13.0-86-generic\|3.13.0-88-generic\|3.13.0-91-generic\|3.13.0-92-generic\|3.13.0-93-generic\|3.13.0-95-generic\|3.13.0-96-generic\|3.13.0-98-generic\|3.2.0-101-generic\|3.2.0-101-generic-pae\|3.2.0-101-virtual\|3.2.0-102-generic\|3.2.0-102-generic-pae\|3.2.0-102-virtual\|3.2.0-104-generic\|3.2.0-104-generic-pae\|3.2.0-104-virtual\|3.2.0-105-generic\|3.2.0-105-generic-pae\|3.2.0-105-virtual\|3.2.0-106-generic\|3.2.0-106-generic-pae\|3.2.0-106-virtual\|3.2.0-107-generic\|3.2.0-107-generic-pae\|3.2.0-107-virtual\|3.2.0-109-generic\|3.2.0-109-generic-pae\|3.2.0-109-virtual\|3.2.0-110-generic\|3.2.0-110-generic-pae\|3.2.0-110-virtual\|3.2.0-111-generic\|3.2.0-111-generic-pae\|3.2.0-111-virtual\|3.2.0-1412-omap4\|3.2.0-1602-armadaxp\|3.2.0-23-generic\|3.2.0-23-generic-pae\|3.2.0-23-lowlatency\|3.2.0-23-lowlatency-pae\|3.2.0-23-omap\|3.2.0-23-powerpc-smp\|3.2.0-23-powerpc64-smp\|3.2.0-23-virtual\|3.2.0-24-generic\|3.2.0-24-generic-pae\|3.2.0-24-virtual\|3.2.0-25-generic\|3.2.0-25-generic-pae\|3.2.0-25-virtual\|3.2.0-26-generic\|3.2.0-26-generic-pae\|3.2.0-26-virtual\|3.2.0-27-generic\|3.2.0-27-generic-pae\|3.2.0-27-virtual\|3.2.0-29-generic\|3.2.0-29-generic-pae\|3.2.0-29-virtual\|3.2.0-31-generic\|3.2.0-31-generic-pae\|3.2.0-31-virtual\|3.2.0-32-generic\|3.2.0-32-generic-pae\|3.2.0-32-virtual\|3.2.0-33-generic\|3.2.0-33-generic-pae\|3.2.0-33-lowlatency\|3.2.0-33-lowlatency-pae\|3.2.0-33-virtual\|3.2.0-34-generic\|3.2.0-34-generic-pae\|3.2.0-34-virtual\|3.2.0-35-generic\|3.2.0-35-generic-pae\|3.2.0-35-lowlatency\|3.2.0-35-lowlatency-pae\|3.2.0-35-virtual\|3.2.0-36-generic\|3.2.0-36-generic-pae\|3.2.0-36-lowlatency\|3.2.0-36-lowlatency-pae\|3.2.0-36-virtual\|3.2.0-37-generic\|3.2.0-37-generic-pae\|3.2.0-37-lowlatency\|3.2.0-37-lowlatency-pae\|3.2.0-37-virtual\|3.2.0-38-generic\|3.2.0-38-generic-pae\|3.2.0-38-lowlatency\|3.2.0-38-lowlatency-pae\|3.2.0-38-virtual\|3.2.0-39-generic\|3.2.0-39-generic-pae\|3.2.0-39-lowlatency\|3.2.0-39-lowlatency-pae\|3.2.0-39-virtual\|3.2.0-40-generic\|3.2.0-40-generic-pae\|3.2.0-40-lowlatency\|3.2.0-40-lowlatency-pae\|3.2.0-40-virtual\|3.2.0-41-generic\|3.2.0-41-generic-pae\|3.2.0-41-lowlatency\|3.2.0-41-lowlatency-pae\|3.2.0-41-virtual\|3.2.0-43-generic\|3.2.0-43-generic-pae\|3.2.0-43-virtual\|3.2.0-44-generic\|3.2.0-44-generic-pae\|3.2.0-44-lowlatency\|3.2.0-44-lowlatency-pae\|3.2.0-44-virtual\|3.2.0-45-generic\|3.2.0-45-generic-pae\|3.2.0-45-virtual\|3.2.0-48-generic\|3.2.0-48-generic-pae\|3.2.0-48-lowlatency\|3.2.0-48-lowlatency-pae\|3.2.0-48-virtual\|3.2.0-51-generic\|3.2.0-51-generic-pae\|3.2.0-51-lowlatency\|3.2.0-51-lowlatency-pae\|3.2.0-51-virtual\|3.2.0-52-generic\|3.2.0-52-generic-pae\|3.2.0-52-lowlatency\|3.2.0-52-lowlatency-pae\|3.2.0-52-virtual\|3.2.0-53-generic"
kernelDCW_Ubuntu_Precise_2="3.2.0-53-generic-pae\|3.2.0-53-lowlatency\|3.2.0-53-lowlatency-pae\|3.2.0-53-virtual\|3.2.0-54-generic\|3.2.0-54-generic-pae\|3.2.0-54-lowlatency\|3.2.0-54-lowlatency-pae\|3.2.0-54-virtual\|3.2.0-55-generic\|3.2.0-55-generic-pae\|3.2.0-55-lowlatency\|3.2.0-55-lowlatency-pae\|3.2.0-55-virtual\|3.2.0-56-generic\|3.2.0-56-generic-pae\|3.2.0-56-lowlatency\|3.2.0-56-lowlatency-pae\|3.2.0-56-virtual\|3.2.0-57-generic\|3.2.0-57-generic-pae\|3.2.0-57-lowlatency\|3.2.0-57-lowlatency-pae\|3.2.0-57-virtual\|3.2.0-58-generic\|3.2.0-58-generic-pae\|3.2.0-58-lowlatency\|3.2.0-58-lowlatency-pae\|3.2.0-58-virtual\|3.2.0-59-generic\|3.2.0-59-generic-pae\|3.2.0-59-lowlatency\|3.2.0-59-lowlatency-pae\|3.2.0-59-virtual\|3.2.0-60-generic\|3.2.0-60-generic-pae\|3.2.0-60-lowlatency\|3.2.0-60-lowlatency-pae\|3.2.0-60-virtual\|3.2.0-61-generic\|3.2.0-61-generic-pae\|3.2.0-61-virtual\|3.2.0-63-generic\|3.2.0-63-generic-pae\|3.2.0-63-lowlatency\|3.2.0-63-lowlatency-pae\|3.2.0-63-virtual\|3.2.0-64-generic\|3.2.0-64-generic-pae\|3.2.0-64-lowlatency\|3.2.0-64-lowlatency-pae\|3.2.0-64-virtual\|3.2.0-65-generic\|3.2.0-65-generic-pae\|3.2.0-65-lowlatency\|3.2.0-65-lowlatency-pae\|3.2.0-65-virtual\|3.2.0-67-generic\|3.2.0-67-generic-pae\|3.2.0-67-lowlatency\|3.2.0-67-lowlatency-pae\|3.2.0-67-virtual\|3.2.0-68-generic\|3.2.0-68-generic-pae\|3.2.0-68-lowlatency\|3.2.0-68-lowlatency-pae\|3.2.0-68-virtual\|3.2.0-69-generic\|3.2.0-69-generic-pae\|3.2.0-69-lowlatency\|3.2.0-69-lowlatency-pae\|3.2.0-69-virtual\|3.2.0-70-generic\|3.2.0-70-generic-pae\|3.2.0-70-lowlatency\|3.2.0-70-lowlatency-pae\|3.2.0-70-virtual\|3.2.0-72-generic\|3.2.0-72-generic-pae\|3.2.0-72-lowlatency\|3.2.0-72-lowlatency-pae\|3.2.0-72-virtual\|3.2.0-73-generic\|3.2.0-73-generic-pae\|3.2.0-73-lowlatency\|3.2.0-73-lowlatency-pae\|3.2.0-73-virtual\|3.2.0-74-generic\|3.2.0-74-generic-pae\|3.2.0-74-lowlatency\|3.2.0-74-lowlatency-pae\|3.2.0-74-virtual\|3.2.0-75-generic\|3.2.0-75-generic-pae\|3.2.0-75-lowlatency\|3.2.0-75-lowlatency-pae\|3.2.0-75-virtual\|3.2.0-76-generic\|3.2.0-76-generic-pae\|3.2.0-76-lowlatency\|3.2.0-76-lowlatency-pae\|3.2.0-76-virtual\|3.2.0-77-generic\|3.2.0-77-generic-pae\|3.2.0-77-lowlatency\|3.2.0-77-lowlatency-pae\|3.2.0-77-virtual\|3.2.0-79-generic\|3.2.0-79-generic-pae\|3.2.0-79-lowlatency\|3.2.0-79-lowlatency-pae\|3.2.0-79-virtual\|3.2.0-80-generic\|3.2.0-80-generic-pae\|3.2.0-80-lowlatency\|3.2.0-80-lowlatency-pae\|3.2.0-80-virtual\|3.2.0-82-generic\|3.2.0-82-generic-pae\|3.2.0-82-lowlatency\|3.2.0-82-lowlatency-pae\|3.2.0-82-virtual\|3.2.0-83-generic\|3.2.0-83-generic-pae\|3.2.0-83-virtual\|3.2.0-84-generic\|3.2.0-84-generic-pae\|3.2.0-84-virtual\|3.2.0-85-generic\|3.2.0-85-generic-pae\|3.2.0-85-virtual\|3.2.0-86-generic\|3.2.0-86-generic-pae\|3.2.0-86-virtual\|3.2.0-87-generic\|3.2.0-87-generic-pae\|3.2.0-87-virtual\|3.2.0-88-generic\|3.2.0-88-generic-pae\|3.2.0-88-virtual\|3.2.0-89-generic\|3.2.0-89-generic-pae\|3.2.0-89-virtual\|3.2.0-90-generic\|3.2.0-90-generic-pae\|3.2.0-90-virtual\|3.2.0-91-generic\|3.2.0-91-generic-pae\|3.2.0-91-virtual\|3.2.0-92-generic\|3.2.0-92-generic-pae\|3.2.0-92-virtual\|3.2.0-93-generic\|3.2.0-93-generic-pae\|3.2.0-93-virtual\|3.2.0-94-generic\|3.2.0-94-generic-pae\|3.2.0-94-virtual\|3.2.0-95-generic\|3.2.0-95-generic-pae\|3.2.0-95-virtual\|3.2.0-96-generic\|3.2.0-96-generic-pae\|3.2.0-96-virtual\|3.2.0-97-generic\|3.2.0-97-generic-pae\|3.2.0-97-virtual\|3.2.0-98-generic\|3.2.0-98-generic-pae\|3.2.0-98-virtual\|3.2.0-99-generic\|3.2.0-99-generic-pae\|3.2.0-99-virtual\|3.5.0-40-generic\|3.5.0-41-generic\|3.5.0-42-generic\|3.5.0-43-generic\|3.5.0-44-generic\|3.5.0-45-generic\|3.5.0-46-generic\|3.5.0-49-generic\|3.5.0-51-generic\|3.5.0-52-generic\|3.5.0-54-generic\|3.8.0-19-generic\|3.8.0-21-generic\|3.8.0-22-generic\|3.8.0-23-generic\|3.8.0-27-generic\|3.8.0-29-generic\|3.8.0-30-generic\|3.8.0-31-generic\|3.8.0-32-generic\|3.8.0-33-generic\|3.8.0-34-generic\|3.8.0-35-generic\|3.8.0-36-generic\|3.8.0-37-generic\|3.8.0-38-generic\|3.8.0-39-generic\|3.8.0-41-generic\|3.8.0-42-generic"
kernelDCW_Ubuntu_Trusty_1="3.13.0-24-generic\|3.13.0-24-generic-lpae\|3.13.0-24-lowlatency\|3.13.0-24-powerpc-e500\|3.13.0-24-powerpc-e500mc\|3.13.0-24-powerpc-smp\|3.13.0-24-powerpc64-emb\|3.13.0-24-powerpc64-smp\|3.13.0-27-generic\|3.13.0-27-lowlatency\|3.13.0-29-generic\|3.13.0-29-lowlatency\|3.13.0-3-exynos5\|3.13.0-30-generic\|3.13.0-30-lowlatency\|3.13.0-32-generic\|3.13.0-32-lowlatency\|3.13.0-33-generic\|3.13.0-33-lowlatency\|3.13.0-34-generic\|3.13.0-34-lowlatency\|3.13.0-35-generic\|3.13.0-35-lowlatency\|3.13.0-36-generic\|3.13.0-36-lowlatency\|3.13.0-37-generic\|3.13.0-37-lowlatency\|3.13.0-39-generic\|3.13.0-39-lowlatency\|3.13.0-40-generic\|3.13.0-40-lowlatency\|3.13.0-41-generic\|3.13.0-41-lowlatency\|3.13.0-43-generic\|3.13.0-43-lowlatency\|3.13.0-44-generic\|3.13.0-44-lowlatency\|3.13.0-46-generic\|3.13.0-46-lowlatency\|3.13.0-48-generic\|3.13.0-48-lowlatency\|3.13.0-49-generic\|3.13.0-49-lowlatency\|3.13.0-51-generic\|3.13.0-51-lowlatency\|3.13.0-52-generic\|3.13.0-52-lowlatency\|3.13.0-53-generic\|3.13.0-53-lowlatency\|3.13.0-54-generic\|3.13.0-54-lowlatency\|3.13.0-55-generic\|3.13.0-55-lowlatency\|3.13.0-57-generic\|3.13.0-57-lowlatency\|3.13.0-58-generic\|3.13.0-58-lowlatency\|3.13.0-59-generic\|3.13.0-59-lowlatency\|3.13.0-61-generic\|3.13.0-61-lowlatency\|3.13.0-62-generic\|3.13.0-62-lowlatency\|3.13.0-63-generic\|3.13.0-63-lowlatency\|3.13.0-65-generic\|3.13.0-65-lowlatency\|3.13.0-66-generic\|3.13.0-66-lowlatency\|3.13.0-67-generic\|3.13.0-67-lowlatency\|3.13.0-68-generic\|3.13.0-68-lowlatency\|3.13.0-70-generic\|3.13.0-70-lowlatency\|3.13.0-71-generic\|3.13.0-71-lowlatency\|3.13.0-73-generic\|3.13.0-73-lowlatency\|3.13.0-74-generic\|3.13.0-74-lowlatency\|3.13.0-76-generic\|3.13.0-76-lowlatency\|3.13.0-77-generic\|3.13.0-77-lowlatency\|3.13.0-79-generic\|3.13.0-79-lowlatency\|3.13.0-83-generic\|3.13.0-83-lowlatency\|3.13.0-85-generic\|3.13.0-85-lowlatency\|3.13.0-86-generic\|3.13.0-86-lowlatency\|3.13.0-87-generic\|3.13.0-87-lowlatency\|3.13.0-88-generic\|3.13.0-88-lowlatency\|3.13.0-91-generic\|3.13.0-91-lowlatency\|3.13.0-92-generic\|3.13.0-92-lowlatency\|3.13.0-93-generic\|3.13.0-93-lowlatency\|3.13.0-95-generic\|3.13.0-95-lowlatency\|3.13.0-96-generic\|3.13.0-96-lowlatency\|3.13.0-98-generic\|3.13.0-98-lowlatency\|3.16.0-25-generic\|3.16.0-25-lowlatency\|3.16.0-26-generic\|3.16.0-26-lowlatency\|3.16.0-28-generic\|3.16.0-28-lowlatency\|3.16.0-29-generic\|3.16.0-29-lowlatency\|3.16.0-31-generic\|3.16.0-31-lowlatency\|3.16.0-33-generic\|3.16.0-33-lowlatency\|3.16.0-34-generic\|3.16.0-34-lowlatency\|3.16.0-36-generic\|3.16.0-36-lowlatency\|3.16.0-37-generic\|3.16.0-37-lowlatency\|3.16.0-38-generic\|3.16.0-38-lowlatency\|3.16.0-39-generic\|3.16.0-39-lowlatency\|3.16.0-41-generic\|3.16.0-41-lowlatency\|3.16.0-43-generic\|3.16.0-43-lowlatency\|3.16.0-44-generic\|3.16.0-44-lowlatency\|3.16.0-45-generic"
kernelDCW_Ubuntu_Trusty_2="3.16.0-45-lowlatency\|3.16.0-46-generic\|3.16.0-46-lowlatency\|3.16.0-48-generic\|3.16.0-48-lowlatency\|3.16.0-49-generic\|3.16.0-49-lowlatency\|3.16.0-50-generic\|3.16.0-50-lowlatency\|3.16.0-51-generic\|3.16.0-51-lowlatency\|3.16.0-52-generic\|3.16.0-52-lowlatency\|3.16.0-53-generic\|3.16.0-53-lowlatency\|3.16.0-55-generic\|3.16.0-55-lowlatency\|3.16.0-56-generic\|3.16.0-56-lowlatency\|3.16.0-57-generic\|3.16.0-57-lowlatency\|3.16.0-59-generic\|3.16.0-59-lowlatency\|3.16.0-60-generic\|3.16.0-60-lowlatency\|3.16.0-62-generic\|3.16.0-62-lowlatency\|3.16.0-67-generic\|3.16.0-67-lowlatency\|3.16.0-69-generic\|3.16.0-69-lowlatency\|3.16.0-70-generic\|3.16.0-70-lowlatency\|3.16.0-71-generic\|3.16.0-71-lowlatency\|3.16.0-73-generic\|3.16.0-73-lowlatency\|3.16.0-76-generic\|3.16.0-76-lowlatency\|3.16.0-77-generic\|3.16.0-77-lowlatency\|3.19.0-20-generic\|3.19.0-20-lowlatency\|3.19.0-21-generic\|3.19.0-21-lowlatency\|3.19.0-22-generic\|3.19.0-22-lowlatency\|3.19.0-23-generic\|3.19.0-23-lowlatency\|3.19.0-25-generic\|3.19.0-25-lowlatency\|3.19.0-26-generic\|3.19.0-26-lowlatency\|3.19.0-28-generic\|3.19.0-28-lowlatency\|3.19.0-30-generic\|3.19.0-30-lowlatency\|3.19.0-31-generic\|3.19.0-31-lowlatency\|3.19.0-32-generic\|3.19.0-32-lowlatency\|3.19.0-33-generic\|3.19.0-33-lowlatency\|3.19.0-37-generic\|3.19.0-37-lowlatency\|3.19.0-39-generic\|3.19.0-39-lowlatency\|3.19.0-41-generic\|3.19.0-41-lowlatency\|3.19.0-42-generic\|3.19.0-42-lowlatency\|3.19.0-43-generic\|3.19.0-43-lowlatency\|3.19.0-47-generic\|3.19.0-47-lowlatency\|3.19.0-49-generic\|3.19.0-49-lowlatency\|3.19.0-51-generic\|3.19.0-51-lowlatency\|3.19.0-56-generic\|3.19.0-56-lowlatency\|3.19.0-58-generic\|3.19.0-58-lowlatency\|3.19.0-59-generic\|3.19.0-59-lowlatency\|3.19.0-61-generic\|3.19.0-61-lowlatency\|3.19.0-64-generic\|3.19.0-64-lowlatency\|3.19.0-65-generic\|3.19.0-65-lowlatency\|3.19.0-66-generic\|3.19.0-66-lowlatency\|3.19.0-68-generic\|3.19.0-68-lowlatency\|3.19.0-69-generic\|3.19.0-69-lowlatency\|3.19.0-71-generic\|3.19.0-71-lowlatency\|3.4.0-5-chromebook\|4.2.0-18-generic\|4.2.0-18-lowlatency\|4.2.0-19-generic\|4.2.0-19-lowlatency\|4.2.0-21-generic\|4.2.0-21-lowlatency\|4.2.0-22-generic\|4.2.0-22-lowlatency\|4.2.0-23-generic\|4.2.0-23-lowlatency\|4.2.0-25-generic\|4.2.0-25-lowlatency\|4.2.0-27-generic\|4.2.0-27-lowlatency\|4.2.0-30-generic\|4.2.0-30-lowlatency\|4.2.0-34-generic\|4.2.0-34-lowlatency\|4.2.0-35-generic\|4.2.0-35-lowlatency\|4.2.0-36-generic\|4.2.0-36-lowlatency\|4.2.0-38-generic\|4.2.0-38-lowlatency\|4.2.0-41-generic\|4.2.0-41-lowlatency\|4.4.0-21-generic\|4.4.0-21-lowlatency\|4.4.0-22-generic\|4.4.0-22-lowlatency\|4.4.0-24-generic\|4.4.0-24-lowlatency\|4.4.0-28-generic\|4.4.0-28-lowlatency\|4.4.0-31-generic\|4.4.0-31-lowlatency\|4.4.0-34-generic\|4.4.0-34-lowlatency\|4.4.0-36-generic\|4.4.0-36-lowlatency\|4.4.0-38-generic\|4.4.0-38-lowlatency\|4.4.0-42-generic\|4.4.0-42-lowlatency"
kernelDCW_Ubuntu_Xenial="4.4.0-1009-raspi2\|4.4.0-1012-snapdragon\|4.4.0-21-generic\|4.4.0-21-generic-lpae\|4.4.0-21-lowlatency\|4.4.0-21-powerpc-e500mc\|4.4.0-21-powerpc-smp\|4.4.0-21-powerpc64-emb\|4.4.0-21-powerpc64-smp\|4.4.0-22-generic\|4.4.0-22-lowlatency\|4.4.0-24-generic\|4.4.0-24-lowlatency\|4.4.0-28-generic\|4.4.0-28-lowlatency\|4.4.0-31-generic\|4.4.0-31-lowlatency\|4.4.0-34-generic\|4.4.0-34-lowlatency\|4.4.0-36-generic\|4.4.0-36-lowlatency\|4.4.0-38-generic\|4.4.0-38-lowlatency\|4.4.0-42-generic\|4.4.0-42-lowlatency"
kernelDCW_Rhel5="2.6.24.7-74.el5rt\|2.6.24.7-81.el5rt\|2.6.24.7-93.el5rt\|2.6.24.7-101.el5rt\|2.6.24.7-108.el5rt\|2.6.24.7-111.el5rt\|2.6.24.7-117.el5rt\|2.6.24.7-126.el5rt\|2.6.24.7-132.el5rt\|2.6.24.7-137.el5rt\|2.6.24.7-139.el5rt\|2.6.24.7-146.el5rt\|2.6.24.7-149.el5rt\|2.6.24.7-161.el5rt\|2.6.24.7-169.el5rt\|2.6.33.7-rt29.45.el5rt\|2.6.33.7-rt29.47.el5rt\|2.6.33.7-rt29.55.el5rt\|2.6.33.9-rt31.64.el5rt\|2.6.33.9-rt31.67.el5rt\|2.6.33.9-rt31.86.el5rt\|2.6.18-8.1.1.el5\|2.6.18-8.1.3.el5\|2.6.18-8.1.4.el5\|2.6.18-8.1.6.el5\|2.6.18-8.1.8.el5\|2.6.18-8.1.10.el5\|2.6.18-8.1.14.el5\|2.6.18-8.1.15.el5\|2.6.18-53.el5\|2.6.18-53.1.4.el5\|2.6.18-53.1.6.el5\|2.6.18-53.1.13.el5\|2.6.18-53.1.14.el5\|2.6.18-53.1.19.el5\|2.6.18-53.1.21.el5\|2.6.18-92.el5\|2.6.18-92.1.1.el5\|2.6.18-92.1.6.el5\|2.6.18-92.1.10.el5\|2.6.18-92.1.13.el5\|2.6.18-92.1.18.el5\|2.6.18-92.1.22.el5\|2.6.18-92.1.24.el5\|2.6.18-92.1.26.el5\|2.6.18-92.1.27.el5\|2.6.18-92.1.28.el5\|2.6.18-92.1.29.el5\|2.6.18-92.1.32.el5\|2.6.18-92.1.35.el5\|2.6.18-92.1.38.el5\|2.6.18-128.el5\|2.6.18-128.1.1.el5\|2.6.18-128.1.6.el5\|2.6.18-128.1.10.el5\|2.6.18-128.1.14.el5\|2.6.18-128.1.16.el5\|2.6.18-128.2.1.el5\|2.6.18-128.4.1.el5\|2.6.18-128.4.1.el5\|2.6.18-128.7.1.el5\|2.6.18-128.8.1.el5\|2.6.18-128.11.1.el5\|2.6.18-128.12.1.el5\|2.6.18-128.14.1.el5\|2.6.18-128.16.1.el5\|2.6.18-128.17.1.el5\|2.6.18-128.18.1.el5\|2.6.18-128.23.1.el5\|2.6.18-128.23.2.el5\|2.6.18-128.25.1.el5\|2.6.18-128.26.1.el5\|2.6.18-128.27.1.el5\|2.6.18-128.29.1.el5\|2.6.18-128.30.1.el5\|2.6.18-128.31.1.el5\|2.6.18-128.32.1.el5\|2.6.18-128.35.1.el5\|2.6.18-128.36.1.el5\|2.6.18-128.37.1.el5\|2.6.18-128.38.1.el5\|2.6.18-128.39.1.el5\|2.6.18-128.40.1.el5\|2.6.18-128.41.1.el5\|2.6.18-164.el5\|2.6.18-164.2.1.el5\|2.6.18-164.6.1.el5\|2.6.18-164.9.1.el5\|2.6.18-164.10.1.el5\|2.6.18-164.11.1.el5\|2.6.18-164.15.1.el5\|2.6.18-164.17.1.el5\|2.6.18-164.19.1.el5\|2.6.18-164.21.1.el5\|2.6.18-164.25.1.el5\|2.6.18-164.25.2.el5\|2.6.18-164.28.1.el5\|2.6.18-164.30.1.el5\|2.6.18-164.32.1.el5\|2.6.18-164.34.1.el5\|2.6.18-164.36.1.el5\|2.6.18-164.37.1.el5\|2.6.18-164.38.1.el5\|2.6.18-194.el5\|2.6.18-194.3.1.el5\|2.6.18-194.8.1.el5\|2.6.18-194.11.1.el5\|2.6.18-194.11.3.el5\|2.6.18-194.11.4.el5\|2.6.18-194.17.1.el5\|2.6.18-194.17.4.el5\|2.6.18-194.26.1.el5\|2.6.18-194.32.1.el5\|2.6.18-238.el5\|2.6.18-238.1.1.el5\|2.6.18-238.5.1.el5\|2.6.18-238.9.1.el5\|2.6.18-238.12.1.el5\|2.6.18-238.19.1.el5\|2.6.18-238.21.1.el5\|2.6.18-238.27.1.el5\|2.6.18-238.28.1.el5\|2.6.18-238.31.1.el5\|2.6.18-238.33.1.el5\|2.6.18-238.35.1.el5\|2.6.18-238.37.1.el5\|2.6.18-238.39.1.el5\|2.6.18-238.40.1.el5\|2.6.18-238.44.1.el5\|2.6.18-238.45.1.el5\|2.6.18-238.47.1.el5\|2.6.18-238.48.1.el5\|2.6.18-238.49.1.el5\|2.6.18-238.50.1.el5\|2.6.18-238.51.1.el5\|2.6.18-238.52.1.el5\|2.6.18-238.53.1.el5\|2.6.18-238.54.1.el5\|2.6.18-238.55.1.el5\|2.6.18-238.56.1.el5\|2.6.18-274.el5\|2.6.18-274.3.1.el5\|2.6.18-274.7.1.el5\|2.6.18-274.12.1.el5\|2.6.18-274.17.1.el5\|2.6.18-274.18.1.el5\|2.6.18-308.el5\|2.6.18-308.1.1.el5\|2.6.18-308.4.1.el5\|2.6.18-308.8.1.el5\|2.6.18-308.8.2.el5\|2.6.18-308.11.1.el5\|2.6.18-308.13.1.el5\|2.6.18-308.16.1.el5\|2.6.18-308.20.1.el5\|2.6.18-308.24.1.el5\|2.6.18-348.el5\|2.6.18-348.1.1.el5\|2.6.18-348.2.1.el5\|2.6.18-348.3.1.el5\|2.6.18-348.4.1.el5\|2.6.18-348.6.1.el5\|2.6.18-348.12.1.el5\|2.6.18-348.16.1.el5\|2.6.18-348.18.1.el5\|2.6.18-348.19.1.el5\|2.6.18-348.21.1.el5\|2.6.18-348.22.1.el5\|2.6.18-348.23.1.el5\|2.6.18-348.25.1.el5\|2.6.18-348.27.1.el5\|2.6.18-348.28.1.el5\|2.6.18-348.29.1.el5\|2.6.18-348.30.1.el5\|2.6.18-348.31.2.el5\|2.6.18-371.el5\|2.6.18-371.1.2.el5\|2.6.18-371.3.1.el5\|2.6.18-371.4.1.el5\|2.6.18-371.6.1.el5\|2.6.18-371.8.1.el5\|2.6.18-371.9.1.el5\|2.6.18-371.11.1.el5\|2.6.18-371.12.1.el5\|2.6.18-398.el5\|2.6.18-400.el5\|2.6.18-400.1.1.el5\|2.6.18-402.el5\|2.6.18-404.el5\|2.6.18-406.el5\|2.6.18-407.el5\|2.6.18-408.el5\|2.6.18-409.el5\|2.6.18-410.el5\|2.6.18-411.el5\|2.6.18-412.el5"
kernelDCW_Rhel6_1="2.6.33.9-rt31.66.el6rt\|2.6.33.9-rt31.74.el6rt\|2.6.33.9-rt31.75.el6rt\|2.6.33.9-rt31.79.el6rt\|3.0.9-rt26.45.el6rt\|3.0.9-rt26.46.el6rt\|3.0.18-rt34.53.el6rt\|3.0.25-rt44.57.el6rt\|3.0.30-rt50.62.el6rt\|3.0.36-rt57.66.el6rt\|3.2.23-rt37.56.el6rt\|3.2.33-rt50.66.el6rt\|3.6.11-rt28.20.el6rt\|3.6.11-rt30.25.el6rt\|3.6.11.2-rt33.39.el6rt\|3.6.11.5-rt37.55.el6rt\|3.8.13-rt14.20.el6rt\|3.8.13-rt14.25.el6rt\|3.8.13-rt27.33.el6rt\|3.8.13-rt27.34.el6rt\|3.8.13-rt27.40.el6rt\|3.10.0-229.rt56.144.el6rt\|3.10.0-229.rt56.147.el6rt\|3.10.0-229.rt56.149.el6rt\|3.10.0-229.rt56.151.el6rt\|3.10.0-229.rt56.153.el6rt\|3.10.0-229.rt56.158.el6rt\|3.10.0-229.rt56.161.el6rt\|3.10.0-229.rt56.162.el6rt\|3.10.0-327.rt56.170.el6rt\|3.10.0-327.rt56.171.el6rt\|3.10.0-327.rt56.176.el6rt\|3.10.0-327.rt56.183.el6rt\|3.10.0-327.rt56.190.el6rt\|3.10.0-327.rt56.194.el6rt\|3.10.0-327.rt56.195.el6rt\|3.10.0-327.rt56.197.el6rt\|3.10.33-rt32.33.el6rt\|3.10.33-rt32.34.el6rt\|3.10.33-rt32.43.el6rt\|3.10.33-rt32.45.el6rt\|3.10.33-rt32.51.el6rt\|3.10.33-rt32.52.el6rt\|3.10.58-rt62.58.el6rt\|3.10.58-rt62.60.el6rt\|2.6.32-71.7.1.el6\|2.6.32-71.14.1.el6\|2.6.32-71.18.1.el6\|2.6.32-71.18.2.el6\|2.6.32-71.24.1.el6\|2.6.32-71.29.1.el6\|2.6.32-71.31.1.el6\|2.6.32-71.34.1.el6\|2.6.32-71.35.1.el6\|2.6.32-71.36.1.el6\|2.6.32-71.37.1.el6\|2.6.32-71.38.1.el6\|2.6.32-71.39.1.el6\|2.6.32-71.40.1.el6\|2.6.32-131.0.15.el6\|2.6.32-131.2.1.el6\|2.6.32-131.4.1.el6\|2.6.32-131.6.1.el6\|2.6.32-131.12.1.el6\|2.6.32-131.17.1.el6\|2.6.32-131.21.1.el6\|2.6.32-131.22.1.el6\|2.6.32-131.25.1.el6\|2.6.32-131.26.1.el6\|2.6.32-131.28.1.el6\|2.6.32-131.29.1.el6\|2.6.32-131.30.1.el6\|2.6.32-131.30.2.el6\|2.6.32-131.33.1.el6\|2.6.32-131.35.1.el6\|2.6.32-131.36.1.el6\|2.6.32-131.37.1.el6\|2.6.32-131.38.1.el6\|2.6.32-131.39.1.el6\|2.6.32-220.el6\|2.6.32-220.2.1.el6\|2.6.32-220.4.1.el6\|2.6.32-220.4.2.el6\|2.6.32-220.4.7.bgq.el6\|2.6.32-220.7.1.el6\|2.6.32-220.7.3.p7ih.el6\|2.6.32-220.7.4.p7ih.el6\|2.6.32-220.7.6.p7ih.el6\|2.6.32-220.7.7.p7ih.el6\|2.6.32-220.13.1.el6\|2.6.32-220.17.1.el6\|2.6.32-220.23.1.el6\|2.6.32-220.24.1.el6\|2.6.32-220.25.1.el6\|2.6.32-220.26.1.el6\|2.6.32-220.28.1.el6\|2.6.32-220.30.1.el6\|2.6.32-220.31.1.el6\|2.6.32-220.32.1.el6\|2.6.32-220.34.1.el6\|2.6.32-220.34.2.el6\|2.6.32-220.38.1.el6\|2.6.32-220.39.1.el6\|2.6.32-220.41.1.el6\|2.6.32-220.42.1.el6\|2.6.32-220.45.1.el6\|2.6.32-220.46.1.el6\|2.6.32-220.48.1.el6\|2.6.32-220.51.1.el6\|2.6.32-220.52.1.el6\|2.6.32-220.53.1.el6\|2.6.32-220.54.1.el6\|2.6.32-220.55.1.el6\|2.6.32-220.56.1.el6\|2.6.32-220.57.1.el6\|2.6.32-220.58.1.el6\|2.6.32-220.60.2.el6\|2.6.32-220.62.1.el6\|2.6.32-220.63.2.el6\|2.6.32-220.64.1.el6\|2.6.32-220.65.1.el6\|2.6.32-220.66.1.el6\|2.6.32-220.67.1.el6\|2.6.32-279.el6\|2.6.32-279.1.1.el6\|2.6.32-279.2.1.el6\|2.6.32-279.5.1.el6\|2.6.32-279.5.2.el6\|2.6.32-279.9.1.el6\|2.6.32-279.11.1.el6\|2.6.32-279.14.1.bgq.el6\|2.6.32-279.14.1.el6\|2.6.32-279.19.1.el6\|2.6.32-279.22.1.el6\|2.6.32-279.23.1.el6\|2.6.32-279.25.1.el6\|2.6.32-279.25.2.el6\|2.6.32-279.31.1.el6\|2.6.32-279.33.1.el6\|2.6.32-279.34.1.el6\|2.6.32-279.37.2.el6\|2.6.32-279.39.1.el6"
kernelDCW_Rhel6_2="2.6.32-279.41.1.el6\|2.6.32-279.42.1.el6\|2.6.32-279.43.1.el6\|2.6.32-279.43.2.el6\|2.6.32-279.46.1.el6\|2.6.32-358.el6\|2.6.32-358.0.1.el6\|2.6.32-358.2.1.el6\|2.6.32-358.6.1.el6\|2.6.32-358.6.2.el6\|2.6.32-358.6.3.p7ih.el6\|2.6.32-358.11.1.bgq.el6\|2.6.32-358.11.1.el6\|2.6.32-358.14.1.el6\|2.6.32-358.18.1.el6\|2.6.32-358.23.2.el6\|2.6.32-358.28.1.el6\|2.6.32-358.32.3.el6\|2.6.32-358.37.1.el6\|2.6.32-358.41.1.el6\|2.6.32-358.44.1.el6\|2.6.32-358.46.1.el6\|2.6.32-358.46.2.el6\|2.6.32-358.48.1.el6\|2.6.32-358.49.1.el6\|2.6.32-358.51.1.el6\|2.6.32-358.51.2.el6\|2.6.32-358.55.1.el6\|2.6.32-358.56.1.el6\|2.6.32-358.59.1.el6\|2.6.32-358.61.1.el6\|2.6.32-358.62.1.el6\|2.6.32-358.65.1.el6\|2.6.32-358.67.1.el6\|2.6.32-358.68.1.el6\|2.6.32-358.69.1.el6\|2.6.32-358.70.1.el6\|2.6.32-358.71.1.el6\|2.6.32-358.72.1.el6\|2.6.32-358.73.1.el6\|2.6.32-358.111.1.openstack.el6\|2.6.32-358.114.1.openstack.el6\|2.6.32-358.118.1.openstack.el6\|2.6.32-358.123.4.openstack.el6\|2.6.32-431.el6\|2.6.32-431.1.1.bgq.el6\|2.6.32-431.1.2.el6\|2.6.32-431.3.1.el6\|2.6.32-431.5.1.el6\|2.6.32-431.11.2.el6\|2.6.32-431.17.1.el6\|2.6.32-431.20.3.el6\|2.6.32-431.20.5.el6\|2.6.32-431.23.3.el6\|2.6.32-431.29.2.el6\|2.6.32-431.37.1.el6\|2.6.32-431.40.1.el6\|2.6.32-431.40.2.el6\|2.6.32-431.46.2.el6\|2.6.32-431.50.1.el6\|2.6.32-431.53.2.el6\|2.6.32-431.56.1.el6\|2.6.32-431.59.1.el6\|2.6.32-431.61.2.el6\|2.6.32-431.64.1.el6\|2.6.32-431.66.1.el6\|2.6.32-431.68.1.el6\|2.6.32-431.69.1.el6\|2.6.32-431.70.1.el6\|2.6.32-431.71.1.el6\|2.6.32-431.72.1.el6\|2.6.32-431.73.2.el6\|2.6.32-431.74.1.el6\|2.6.32-504.el6\|2.6.32-504.1.3.el6\|2.6.32-504.3.3.el6\|2.6.32-504.8.1.el6\|2.6.32-504.8.2.bgq.el6\|2.6.32-504.12.2.el6\|2.6.32-504.16.2.el6\|2.6.32-504.23.4.el6\|2.6.32-504.30.3.el6\|2.6.32-504.30.5.p7ih.el6\|2.6.32-504.33.2.el6\|2.6.32-504.36.1.el6\|2.6.32-504.38.1.el6\|2.6.32-504.40.1.el6\|2.6.32-504.43.1.el6\|2.6.32-504.46.1.el6\|2.6.32-504.49.1.el6\|2.6.32-504.50.1.el6\|2.6.32-504.51.1.el6\|2.6.32-504.52.1.el6\|2.6.32-573.el6\|2.6.32-573.1.1.el6\|2.6.32-573.3.1.el6\|2.6.32-573.4.2.bgq.el6\|2.6.32-573.7.1.el6\|2.6.32-573.8.1.el6\|2.6.32-573.12.1.el6\|2.6.32-573.18.1.el6\|2.6.32-573.22.1.el6\|2.6.32-573.26.1.el6\|2.6.32-573.30.1.el6\|2.6.32-573.32.1.el6\|2.6.32-573.34.1.el6\|2.6.32-642.el6\|2.6.32-642.1.1.el6\|2.6.32-642.3.1.el6\|2.6.32-642.4.2.el6\|2.6.32-642.6.1.el6"
kernelDCW_Rhel7="3.10.0-229.rt56.141.el7\|3.10.0-229.1.2.rt56.141.2.el7_1\|3.10.0-229.4.2.rt56.141.6.el7_1\|3.10.0-229.7.2.rt56.141.6.el7_1\|3.10.0-229.11.1.rt56.141.11.el7_1\|3.10.0-229.14.1.rt56.141.13.el7_1\|3.10.0-229.20.1.rt56.141.14.el7_1\|3.10.0-229.rt56.141.el7\|3.10.0-327.rt56.204.el7\|3.10.0-327.4.5.rt56.206.el7_2\|3.10.0-327.10.1.rt56.211.el7_2\|3.10.0-327.13.1.rt56.216.el7_2\|3.10.0-327.18.2.rt56.223.el7_2\|3.10.0-327.22.2.rt56.230.el7_2\|3.10.0-327.28.2.rt56.234.el7_2\|3.10.0-327.28.3.rt56.235.el7\|3.10.0-327.36.1.rt56.237.el7\|3.10.0-123.el7\|3.10.0-123.1.2.el7\|3.10.0-123.4.2.el7\|3.10.0-123.4.4.el7\|3.10.0-123.6.3.el7\|3.10.0-123.8.1.el7\|3.10.0-123.9.2.el7\|3.10.0-123.9.3.el7\|3.10.0-123.13.1.el7\|3.10.0-123.13.2.el7\|3.10.0-123.20.1.el7\|3.10.0-229.el7\|3.10.0-229.1.2.el7\|3.10.0-229.4.2.el7\|3.10.0-229.7.2.el7\|3.10.0-229.11.1.el7\|3.10.0-229.14.1.el7\|3.10.0-229.20.1.el7\|3.10.0-229.24.2.el7\|3.10.0-229.26.2.el7\|3.10.0-229.28.1.el7\|3.10.0-229.30.1.el7\|3.10.0-229.34.1.el7\|3.10.0-229.38.1.el7\|3.10.0-229.40.1.el7\|3.10.0-229.42.1.el7\|3.10.0-327.el7\|3.10.0-327.3.1.el7\|3.10.0-327.4.4.el7\|3.10.0-327.4.5.el7\|3.10.0-327.10.1.el7\|3.10.0-327.13.1.el7\|3.10.0-327.18.2.el7\|3.10.0-327.22.2.el7\|3.10.0-327.28.2.el7\|3.10.0-327.28.3.el7\|3.10.0-327.36.1.el7\|3.10.0-327.36.2.el7\|3.10.0-229.1.2.ael7b\|3.10.0-229.4.2.ael7b\|3.10.0-229.7.2.ael7b\|3.10.0-229.11.1.ael7b\|3.10.0-229.14.1.ael7b\|3.10.0-229.20.1.ael7b\|3.10.0-229.24.2.ael7b\|3.10.0-229.26.2.ael7b\|3.10.0-229.28.1.ael7b\|3.10.0-229.30.1.ael7b\|3.10.0-229.34.1.ael7b\|3.10.0-229.38.1.ael7b\|3.10.0-229.40.1.ael7b\|3.10.0-229.42.1.ael7b\|4.2.0-0.21.el7"

sudovB="1.6.8p9\|1.6.9p18\|1.8.14\|1.8.20\|1.6.9p21\|1.7.2p4\|1\.8\.[0123]$\|1\.3\.[^1]\|1\.4\.\d*\|1\.5\.\d*\|1\.6\.\d*\|1.5$\|1.6$"

mounted=`(mount -l || cat /proc/mounts || cat /proc/self/mounts) 2>/dev/null | grep "^/" | cut -d " " -f1 | tr '\n' '|' | sed 's/|/\\\|/g'``cat /etc/fstab | grep -v "#" | grep " / " | cut -d " " -f 1`
mountG="swap\|/cdrom\|/floppy\|/dev/shm"
notmounted=`cat /etc/fstab | grep "^/" | grep -v $mountG | cut -d " " -f1 | grep -v $mounted | tr '\n' '|' | sed 's/|/\\\|/g'`"ImPoSSssSiBlEee"
mountpermsB="[^o]suid\|[^o]user\|[^o]exec"
mountpermsG="nosuid\|nouser\|noexec"

rootcommon="/init$\|upstart-udev-bridge\|udev\|/getty\|cron\|apache2\|/vmtoolsd\|/VGAuthService"

groupsB="(root)\|(shadow)\|(admin)" #(video) Investigate
groupsVB="(sudo)\|(docker)\|(lxd)\|(wheel)\|(disk)"
knw_grps='(lpadmin)\|(adm)\|(cdrom)\|(plugdev)\|(nogroup)' #https://www.togaware.com/linux/survivor/Standard_Groups.html

sidG="/abuild-sudo$\|/accton$\|/allocate$\|/arping$\|/at$\|/atq$\|/atrm$\|/authpf$\|/authpf-noip$\|/batch$\|/bbsuid$\|/bsd-write$\|/btsockstat$\|/bwrap$\|/cacaocsc$\|/camel-lock-helper-1.2$\|/ccreds_validate$\|/cdrw$\|/chage$\|/check-foreground-console$\|/chrome-sandbox$\|/chsh$\|/cons.saver$\|/crontab$\|/ct$\|/cu$\|/dbus-daemon-launch-helper$\|/deallocate$\|/desktop-create-kmenu$\|/dma$\|/dmcrypt-get-device$\|/doas$\|/dotlockfile$\|/dotlock.mailutils$\|/dtaction$\|/dtfile$\|/dtsession$\|/eject$\|/execabrt-action-install-debuginfo-to-abrt-cache$\|/execdbus-daemon-launch-helper$\|/execdma-mbox-create$\|/execlockspool$\|/execlogin_chpass$\|/execlogin_lchpass$\|/execlogin_passwd$\|/execssh-keysign$\|/execulog-helper$\|/expiry$\|/fdformat$\|/fusermount$\|/gnome-pty-helper$\|/glines$\|/gnibbles$\|/gnobots2$\|/gnome-suspend$\|/gnometris$\|/gnomine$\|/gnotski$\|/gnotravex$\|/gpasswd$\|/gpg$\|/gpio$\|/gtali\|/.hal-mtab-lock$\|/imapd$\|/inndstart$\|/kismet_capture$\|/ksu$\|/list_devices$\|/locate$\|/lock$\|/lockdev$\|/lockfile$\|/login_activ$\|/login_crypto$\|/login_radius$\|/login_skey$\|/login_snk$\|/login_token$\|/login_yubikey$\|/lpd$\|/lpd-port$\|/lppasswd$\|/lpq$\|/lprm$\|/lpset$\|/lxc-user-nic$\|/mahjongg$\|/mail-lock$\|/mailq$\|/mail-touchlock$\|/mail-unlock$\|/mksnap_ffs$\|/mlocate$\|/mlock$\|/mount.cifs$\|/mount.nfs$\|/mount.nfs4$\|/mtr$\|/mutt_dotlock$\|/ncsa_auth$\|/netpr$\|/netreport$\|/netstat$\|/newgidmap$\|/newtask$\|/newuidmap$\|/opieinfo$\|/opiepasswd$\|/pam_auth$\|/pam_extrausers_chkpwd$\|/pam_timestamp_check$\|/pamverifier$\|/pfexec$\|/ping$\|/ping6$\|/pmconfig$\|/polkit-agent-helper-1$\|/polkit-explicit-grant-helper$\|/polkit-grant-helper$\|/polkit-grant-helper-pam$\|/polkit-read-auth-helper$\|/polkit-resolve-exe-helper$\|/polkit-revoke-helper$\|/polkit-set-default-helper$\|/postdrop$\|/postqueue$\|/poweroff$\|/ppp$\|/procmail$\|/pt_chmod$\|/pwdb_chkpwd$\|/quota$\|/remote.unknown$\|/rlogin$\|/rmformat$\|/rnews$\|/sacadm$\|/same-gnome$\|screen.real$\|/sendmail.sendmail$\|/shutdown$\|/skeyaudit$\|/skeyinfo$\|/skeyinit$\|/slocate$\|/smbmnt$\|/smbumount$\|/smpatch$\|/smtpctl$\|/snap-confine$\|/sperl5.8.8$\|/ssh-agent$\|/ssh-keysign$\|/staprun$\|/startinnfeed$\|/stclient$\|/su$\|/suexec$\|/sys-suspend$\|/telnetlogin$\|/timedc$\|/tip$\|/traceroute6$\|/traceroute6.iputils$\|/trpt$\|/tsoldtlabel$\|/tsoljdslabel$\|/tsolxagent$\|/ufsdump$\|/ufsrestore$\|/umount.cifs$\|/umount.nfs$\|/umount.nfs4$\|/unix_chkpwd$\|/uptime$\|/userhelper$\|/userisdnctl$\|/usernetctl$\|/utempter$\|/utmp_update$\|/uucico$\|/uuglist$\|/uuidd$\|/uuname$\|/uusched$\|/uustat$\|/uux$\|/uuxqt$\|/vmware-user-suid-wrapper$\|/vncserver-x11$\|/volrmmount$\|/w$\|/wall$\|/whodo$\|/write$\|/X$\|/Xorg.wrap$\|/xscreensaver$\|/Xsun$\|/Xvnc$"
#Rules: Start path " /", end path "$", divide path and vulnversion "%". SPACE IS ONLY ALLOWED AT BEGINNING, DONT USE IT IN VULN DESCRIPTION
sidB="/apache2%Read_root_passwd__apache2_-f_/etc/shadow\
 /chfn$%SuSE_9.3/10\
 /chkey$%Solaris_2.5.1\
 /chkperm$%Solaris_7.0_\
 /chpass$%OpenBSD_2.7_i386/OpenBSD_2.6_i386/OpenBSD_2.5_1999/08/06/OpenBSD_2.5_1998/05/28/FreeBSD_4.0-RELEASE/FreeBSD_3.5-RELEASE/FreeBSD_3.4-RELEASE/NetBSD_1.4.2\
 /chpasswd$%SquirrelMail\
 /dtappgather$%Solaris_7_<_11_(SPARC/x86)\
 /dtprintinfo$%Solaris_10_(x86)\
 /eject$%FreeBSD_mcweject_0.9/SGI_IRIX_6.2\
 /ibstat%IBM_AIX_Version_6.1/7.1\
 /kcheckpass$%KDE_3.2.0_<-->_3.4.2_(both_included)\
 /kdesud$%KDE_1.1/1.1.1/1.1.2/1.2\
 /keybase-redirector%CentOS_Linux_release_7.4.1708\
 /login$%IBM_AIX_3.2.5/SGI_IRIX_6.4\
 /lpc$%S.u.S.E_Linux_5.2\
 /lpr$%BSD/OS2.1/FreeBSD2.1.5/NeXTstep4.x/IRIX6.4/SunOS4.1.3/4.1.4\
 /mount$%Apple_Mac_OSX(Lion)_Kernel_xnu-1699.32.7_except_xnu-1699.24.8\
 /movemail$%Emacs\
 /netprint$%IRIX_5.3/6.2/6.3/6.4/6.5/6.5.11\
 /newgrp$%HP-UX_10.20\
 /ntfs-3g$%Debian9/8/7/Ubuntu/Gentoo/others/Ubuntu_Server_16.10_and_others\
 /passwd$%Apple_Mac_OSX/Solaris/SPARC_8/9/Sun_Solaris_2.5.1_PAM\
 /pkexec$%rhel_6/Also_check_groups_privileges_and_pkexec_policy\
 /pppd$%Apple_Mac_OSX_10.4.8\
 /pt_chown$%GNU_glibc_2.1/2.1.1_-6\
 /pulseaudio$%(Ubuntu_9.04/Slackware_12.2.0)\
 /rcp$%RedHat_6.2\
 /rdist$%Solaris_10/OpenSolaris\
 /rsh$%Apple_Mac_OSX_10.9.5/10.10.5\
 /screen$%GNU_Screen_4.5.0\
 /screen-4.5.0%GNU_Screen_4.5.0__HIGHLY_PROBABLE_A_PRIVILEGE_ESCALATION_VECTOR\
 /sdtcm_convert$%Sun_Solaris_7.0\
 /sendmail$%Sendmail_8.10.1/Sendmail_8.11.x/Linux_Kernel_2.2.x_2.4.0-test1_(SGI_ProPack_1.2/1.3)\
 /sudo$\
 /sudoedit$%Sudo/SudoEdit_1.6.9p21/1.7.2p4/(RHEL_5/6/7/Ubuntu)/Sudo<=1.8.14\
 /tmux%Tmux_1.3_1.4_privesc
 /traceroute$%LBL_Traceroute_[2000-11-15]\
 /umount$%BSD/Linux[1996-08-13]\
 /umount-loop$%Rocks_Clusters<=4.1\
 /uucp$%Taylor_UUCP_1.0.6\
 /XFree86$%XFree86_X11R6_3.3.x/4.0/4.x/3.3\
 /xlock$%BSD/OS_2.1/DG/UX_7.0/Debian_1.3/HP-UX_10.34/IBM_AIX_4.2/SGI_IRIX_6.4/Solaris_2.5.1\
 /xorg$%xorg-x11-server<=1.20.3/AIX_7.1_(6.x_to_7.x_should_be_vulnerable)_X11.base.rte<7.1.5.32\
 /xterm$%Solaris_5.5.1_X11R6.3"
sidVB='/aria2c$\|/arp$\|/ash$\|/awk$\|/base64$\|/bash$\|/busybox$\|/cat$\|/chmod$\|/chown$\|/cp$\|/csh$\|/curl$\|/cut$\|/dash$\|/date$\|/dd$\|/diff$\|/dmsetup$\|/docker$\|/ed$\|/emacs$\|/env$\|/expand$\|/expect$\|/file$\|/find$\|/flock$\|/fmt$\|/fold$\|/gdb$\|/gimp$\|/git$\|/grep$\|/head$\|/ionice$\|/ip$\|/jjs$\|/jq$\|/jrunscript$\|/ksh$\|/ld.so$\|/less$\|/logsave$\|/lua$\|/make$\|/more$\|/mv$\|/mysql$\|/nano$\|/nc$\|/nice$\|/nl$\|/nmap$\|/node$\|/od$\|/openssl$\|/perl$\|/pg$\|/php$\|/pic$\|/pico$\|/python$\|/readelf$\|/rlwrap$\|/rpm$\|/rpmquery$\|/rsync$\|/rvim$\|/scp$\|/sed$\|/setarch$\|/shuf$\|/socat$\|/sort$\|/sqlite3$\|/stdbuf$\|/strace$\|/systemctl$\|/tail$\|/tar$\|/taskset$\|/tclsh$\|/tee$\|/telnet$\|/tftp$\|/time$\|/timeout$\|/ul$\|/unexpand$\|/uniq$\|/unshare$\|/vim$\|/watch$\|/wget$\|/xargs$\|/xxd$\|/zip$\|/zsh$'

sudoVB=" \*\|env_keep+=LD_PRELOAD\|apt-get$\|apt$\|aria2c$\|arp$\|ash$\|awk$\|base64$\|bash$\|busybox$\|cat$\|chmod$\|chown$\|cp$\|cpan$\|cpulimit$\|crontab$\|csh$\|curl$\|cut$\|dash$\|date$\|dd$\|diff$\|dmesg$\|dmsetup$\|dnf$\|docker$\|dpkg$\|easy_install$\|ed$\|emacs$\|env$\|expand$\|expect$\|facter$\|file$\|find$\|flock$\|fmt$\|fold$\|ftp$\|gdb$\|gimp$\|git$\|grep$\|head$\|ionice$\|ip$\|irb$\|jjs$\|journalctl$\|jq$\|jrunscript$\|ksh$\|ld.so$\|less$\|logsave$\|ltrace$\|lua$\|mail$\|make$\|man$\|more$\|mount$\|mtr$\|mv$\|mysql$\|nano$\|nc$\|nice$\|nl$\|nmap$\|node$\|od$\|openssl$\|perl$\|pg$\|php$\|pic$\|pico$\|pip$\|puppet$\|python$\|readelf$\|red$\|rlwrap$\|rpm$\|rpmquery$\|rsync$\|ruby$\|run-mailcap$\|run-parts$\|rvim$\|scp$\|screen$\|script$\|sed$\|service$\|setarch$\|sftp$\|smbclient$\|socat$\|sort$\|sqlite3$\|ssh$\|start-stop-daemon$\|stdbuf$\|strace$\|systemctl$\|tail$\|tar$\|taskset$\|tclsh$\|tcpdump$\|tee$\|telnet$\|tftp$\|time$\|timeout$\|tmux$\|ul$\|unexpand$\|uniq$\|unshare$\|vi$\|vim$\|watch$\|wget$\|wish$\|xargs$\|xxd$\|yum$\|zip$\|zsh$\|zypper$"
sudoB="$(whoami)\|ALL:ALL\|ALL : ALL\|ALL\|NOPASSWD\|/apache2"

sudocapsB="/apt-get\|/apt\|/aria2c\|/arp\|/ash\|/awk\|/base64\|/bash\|/busybox\|/cat\|/chmod\|/chown\|/cp\|/cpan\|/cpulimit\|/crontab\|/csh\|/curl\|/cut\|/dash\|/date\|/dd\|/diff\|/dmesg\|/dmsetup\|/dnf\|/docker\|/dpkg\|/easy_install\|/ed\|/emacs\|/env\|/expand\|/expect\|/facter\|/file\|/find\|/flock\|/fmt\|/fold\|/ftp\|/gdb\|/gimp\|/git\|/grep\|/head\|/ionice\|/ip\|/irb\|/jjs\|/journalctl\|/jq\|/jrunscript\|/ksh\|/ld.so\|/less\|/logsave\|/ltrace\|/lua\|/mail\|/make\|/man\|/more\|/mount\|/mtr\|/mv\|/mysql\|/nano\|/nc\|/nice\|/nl\|/nmap\|/node\|/od\|/openssl\|/perl\|/pg\|/php\|/pic\|/pico\|/pip\|/puppet\|/python\|/readelf\|/red\|/rlwrap\|/rpm\|/rpmquery\|/rsync\|/ruby\|/run-mailcap\|/run-parts\|/rvim\|/scp\|/screen\|/script\|/sed\|/service\|/setarch\|/sftp\|/smbclient\|/socat\|/sort\|/sqlite3\|/ssh\|/start-stop-daemon\|/stdbuf\|/strace\|/systemctl\|/tail\|/tar\|/taskset\|/tclsh\|/tcpdump\|/tee\|/telnet\|/tftp\|/time\|/timeout\|/tmux\|/ul\|/unexpand\|/uniq\|/unshare\|/vi\|/vim\|/watch\|/wget\|/wish\|/xargs\|/xxd\|/yum\|/zip\|/zsh\|/zypper"
capsB="=ep\|cap_dac_read_search\|cap_dac_override"

OLDPATH=$PATH
ADDPATH=":/usr/local/sbin\
 :/usr/local/bin\
 :/usr/sbin\
 :/usr/bin\
 :/sbin\
 :/bin"
spath=":$PATH"
for P in $ADDPATH; do
  if [ ! -z "${spath##*$P*}" ]; then export PATH="$PATH$P" 2>/dev/null; fi
done
writeB="\.sh$\|\./\|/etc/sysconfig/network-scripts/\|/etc/\|/sys/\|/lib/systemd\|/lib\|/boot\|/root\|/home/\|/var/log/\|/mnt/\|/usr/local/sbin\|/usr/sbin\|/sbin/\|/usr/local/bin\|/usr/bin\|/bin\|/usr/local/games\|/usr/games\|/usr/lib\|/etc/rc.d/\|"
writeVB="/etc/init\|/etc/sys\|/etc/shadow\|/etc/passwd\|/etc/cron\|"`echo $PATH 2>/dev/null| sed 's/:/\\\|/g'`

sh_usrs=`cat /etc/passwd 2>/dev/null | grep -v "^root:" | grep -i "sh$" | cut -d ":" -f 1 | tr '\n' '|' | sed 's/|bin|/|bin[\\\s:]|^bin$|/' | sed 's/|sys|/|sys[\\\s:]|^sys$|/' | sed 's/|daemon|/|daemon[\\\s:]|^daemon$|/' | sed 's/|/\\\|/g'`"ImPoSSssSiBlEee" #Modified bin, sys and daemon so they are not colored everywhere
nosh_usrs=`cat /etc/passwd 2>/dev/null | grep -i -v "sh$" | sort | cut -d ":" -f 1 | tr '\n' '|' | sed 's/|bin|/|bin[\\\s:]|^bin$|/' | sed 's/|/\\\|/g'`"ImPoSSssSiBlEee"
knw_usrs='daemon:\|daemon\s\|^daemon$\|message+\|syslog\|www\|www-data\|mail\|noboby\|Debian-+\|rtkit\|systemd+'
USER=`whoami`
HOME=/home/$USER
GROUPS="ImPoSSssSiBlEee"`groups $USER 2>/dev/null | cut -d ":" -f 2 | tr ' ' '|' | sed 's/|/\\\|/g'`

pwd_inside_history="PASSW\|passw\|root\|sudo\|^su\|pkexec\|^ftp\|mongo\|psql\|mysql\|rdekstop\|xfreerdp\|^ssh\|@"

WF=`find /home /tmp /var /bin /etc /usr /lib /media /mnt /opt /root /dev -type d -maxdepth 2 '(' '(' -user $USER ')' -or '(' -perm -o=w ')' ')' 2>/dev/null | sort`
file=""
for f in $WF; do
  echo '' 2>/dev/null > $f/$filename
  if [ $? -eq 0 ]; then file="$f/$filename"; break; fi;
done;
Wfolders=`echo $WF | tr ' ' '|' | sed 's/|/\\\|/g'`"\|[^\*] \*"

notExtensions="\.tif$\|\.tiff$\|\.gif$\|\.jpeg$\|\.jpg\|\.jif$\|\.jfif$\|\.jp2$\|\.jpx$\|\.j2k$\|\.j2c$\|\.fpx$\|\.pcd$\|\.png$\|\.pdf$\|\.flv$\|\.mp4$\|\.mp3$\|\.gifv$\|\.avi$\|\.mov$\|\.mpeg$\|\.wav$\|\.doc$\|\.docx$\|\.xls$\|\.xlsx$"

TIMEOUT=`which timeout 2>/dev/null`
GCC=`which gcc 2>/dev/null`

pathshG="/0trace.sh\|/blueranger.sh\|/dnsmap-bulk.sh\|/gettext.sh\|/go-rhn.sh\|/gvmap.sh\|/lesspipe.sh\|/mksmbpasswd.sh\|/setuporamysql.sh\|/setup-nsssysinit.sh\|/testacg.sh\|/testlahf.sh\|/url_handler.sh"

notBackup="/tdbbackup$\|/db_hotbackup$"


###########################################
#---------) Checks before start (---------#
###########################################
# --) If root
# --) Writable folder 

if [ "$(/usr/bin/id -u)" -eq "0" ]; then printf $B"[*] "$RED"YOU ARE ALREADY ROOT!!! (nothing is going to be executed)\n"$NC; exit; fi

Wfolder=""
for f in $WF; do
  echo '' 2>/dev/null > $f/$filename
  if [ $? -eq 0 ]; then Wfolder="$f"; file="$f/$filename"; rm -f $f/$filename 2>/dev/null; break; fi;
done;


###########################################
#---------) Parsing parameters (----------#
###########################################
# --) FAST - Do not check 1min of procceses
# --) SUPERFAST - FAST & do not search for special filaes in all the folders

FAST=""
SUPERFAST=""
NOTEXPORT=""
HELP="Enumerate and search Privilege Escalation vectors.\n\t-h To show this message\n\t-f Fast (don't check 1min of processes)\n\t-s SuperFast (don't check 1min of processes and other time consuming checks bypassed)\n\t-n Do not export env variables related with history"

while getopts "h?fv" opt; do
  case "$opt" in
    h|\?) printf $B"$HELP"$NC; exit 0;;
    f)  FAST=1;;
    v)  SUPERFAST=1;;
    n)  NOTEXPORT=1;;
    esac
done


###########################################
#--------------) Functions (--------------#
###########################################

echo_not_found (){
  printf $DG"$1 Not Found\n"$NC
}

echo_no (){
  printf $DG"No\n"$NC
}


###########################################
#---) Exporting history env variables (---#
###########################################

if ! [ "$NOTEXPORT" ]; then
  unset HISTORY HISTFILE HISTSAVE HISTZONE HISTORY HISTLOG WATCH
  export HISTFILE=/dev/null
  export HISTSIZE=0
  export HISTFILESIZE=0
fi


###########################################
#-----------) Starting Output (-----------#
###########################################

echo ""
echo "linpeas $VERSION" | sed "s,.*,${C}[1;94m&${C}[0m,"
printf $B"Linux Privesc Checklist: "$Y"https://book.hacktricks.xyz/linux-unix/linux-privilege-escalation-checklist\n"$NC
echo "LEYEND:" | sed "s,LEYEND,${C}[1;4m&${C}[0m,"
echo "RED/YELLOW: 99% a PE vector" | sed "s,RED/YELLOW,${C}[1;31;103m&${C}[0m,"
echo "RED: You must take a look at it" | sed "s,RED,${C}[1;31m&${C}[0m,"
echo "LightCyan: Users with console" | sed "s,LightCyan,${C}[1;96m&${C}[0m,"
echo "Blue: Users without console & mounted devs" | sed "s,Blue,${C}[1;34m&${C}[0m,"
echo "Green: Common things (users, groups, SUID/SGID, mounts, .sh scripts) " | sed "s,Green,${C}[1;32m&${C}[0m,"
echo "LightMangenta: Your username" | sed "s,LightMangenta,${C}[1;95m&${C}[0m,"
echo ""
echo ""


###########################################
#-----------) Some Basic Info (-----------#
###########################################

printf $B"====================================( "$GREEN"Basic information"$B" )=====================================\n"$NC
printf $LG"OS: "$NC
(cat /proc/version || uname -a ) 2>/dev/null | sed "s,$kernelDCW_Ubuntu_Precise_1,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Precise_2,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Trusty_1,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Trusty_2,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Xenial,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel5,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel6_1,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel6_2,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel7,${C}[1;31;103m&${C}[0m," | sed "s,$kernelB,${C}[1;31m&${C}[0m,"
printf $LG"User & Groups: "$NC
(id || (whoami && groups)) 2>/dev/null | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m,g" | sed "s,$knw_grps,${C}[1;32m&${C}[0m,g" | sed "s,$groupsB,${C}[1;31m&${C}[0m,g" | sed "s,$groupsVB,${C}[1;31;103m&${C}[0m,g" | sed "s,$USER,${C}[1;95m&${C}[0m,g"
printf $LG"Hostname: "$NC
hostname 2>/dev/null
printf $LG"Writable folder: "$NC
echo $Wfolder
echo ""
echo ""


###########################################
#-------------) System Info (-------------#
###########################################
printf $B"====================================( "$GREEN"System Information"$B" )====================================\n"$NC

#-- 1SY) OS
printf $Y"[+] "$GREEN"Operative system\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#kernel-exploits\n"$NC
(cat /proc/version || uname -a ) 2>/dev/null | sed "s,$kernelDCW_Ubuntu_Precise_1,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Precise_2,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Trusty_1,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Trusty_2,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Ubuntu_Xenial,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel5,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel6_1,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel6_2,${C}[1;31;103m&${C}[0m," | sed "s,$kernelDCW_Rhel7,${C}[1;31;103m&${C}[0m," | sed "s,$kernelB,${C}[1;31m&${C}[0m,"
lsb_release -a 2>/dev/null
echo ""

#-- 2SY) Sudo 
printf $Y"[+] "$GREEN"Sudo version\n"$NC
if [ "`which sudo 2>/dev/null`" ]; then
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#sudo-version\n"$NC
  sudo -V 2>/dev/null | grep "Sudo ver" | sed "s,$sudovB,${C}[1;31m&${C}[0m,"
else echo_not_found "sudo"
fi
echo ""

#-- 3SY) PATH
printf $Y"[+] "$GREEN"PATH\n"$NC
printf $B"[i] "$Y"Any writable folder in original PATH? (a new completed path will be exported)\n"$NC
echo $OLDPATH 2>/dev/null | sed "s,$Wfolders\|\.,${C}[1;31;103m&${C}[0m,"
echo "New path exported: $PATH" 2>/dev/null | sed "s,$Wfolders\|\.,${C}[1;31;103m&${C}[0m," 
echo ""

#-- 4SY) Date
printf $Y"[+] "$GREEN"Date\n"$NC
date 2>/dev/null || echo_not_found "date"
echo ""

#-- 5SY) System stats
printf $Y"[+] "$GREEN"System stats\n"$NC
df -h 2>/dev/null || echo_not_found "df"
free 2>/dev/null || echo_not_found "free"
echo ""

#-- 6SY) Environment vars 
printf $Y"[+] "$GREEN"Environment\n"$NC
printf $B"[i] "$Y"Any private information inside environment variables?\n"$NC
(env || set) 2>/dev/null | grep -v "^VERSION=\|pwd_inside_history\|kernelDCW_Ubuntu_Precise_1\|kernelDCW_Ubuntu_Precise_2\|kernelDCW_Ubuntu_Trusty_1\|kernelDCW_Ubuntu_Trusty_2\|kernelDCW_Ubuntu_Xenial\|kernelDCW_Rhel5\|kernelDCW_Rhel6_1\|kernelDCW_Rhel6_2\|kernelDCW_Rhel7\|^sudovB=\|^rootcommon=\|^mounted=\|^mountG=\|^notmounted=\|^mountpermsB=\|^mountpermsG=\|^kernelB=\|^C=\|^RED=\|^GREEN=\|^Y=\|^B=\|^NC=\|TIMEOUT=\|groupsB=\|groupsVB=\|knw_grps=\|sidG=\|sidB=\|sidVB=\|sudoB=\|sudoVB=\|sudocapsB=\|capsB=\|\notExtensions=\|Wfolders=\|writeB=\|writeVB=\|_usrs=\|compiler=\|PWD=\|LS_COLORS=\|pathshG=\|notBackup=" | sed "s,pwd\|passw\|PWD\|PASSW\|Passwd\|Pwd,${C}[1;31m&${C}[0m,g" || echo_not_found "env || set"
echo ""

#-- 7SY) Dmesg
printf $Y"[+] "$GREEN"Looking for Signature verification failed in dmseg\n"$NC
(dmesg 2>/dev/null | grep signature) || echo_not_found
echo ""

#-- 8SY) SElinux
printf $Y"[+] "$GREEN"selinux enabled? .......... "$NC
sestatus 2>/dev/null || echo_not_found "sestatus"

#-- 9SY) Printer
printf $Y"[+] "$GREEN"Printer? .......... "$NC
lpstat -a 2>/dev/null || echo_not_found "lpstat"

#-- 10SY) Container
printf $Y"[+] "$GREEN"Is this a container? .......... "$NC
dockercontainer=`grep -i docker /proc/self/cgroup  2>/dev/null; find / -name "*dockerenv*" -exec ls -la {} \; 2>/dev/null`
lxccontainer=`grep -qa container=lxc /proc/1/environ 2>/dev/null`
if [ "$dockercontainer" ]; then echo "Looks like we're in a Docker container" | sed "s,.*,${C}[1;31m&${C}[0m,";
elif [ "$lxccontainer" ]; then echo "Looks like we're in a LXC container" | sed "s,.*,${C}[1;31m&${C}[0m,";
else echo_no
fi
echo ""
echo ""


###########################################
#---------------) Devices (---------------#
###########################################
printf $B"=========================================( "$GREEN"Devices"$B" )==========================================\n"$NC

#-- 1D) sd in /dev
printf $Y"[+] "$GREEN"Any sd* disk in /dev? (limit 20)\n"$NC
ls /dev 2>/dev/null | grep -i "sd" | sed "s,crypt,${C}[1;31m&${C}[0m," | head -n 20
echo ""

#-- 2D) Unmounted
printf $Y"[+] "$GREEN"Unmounted file-system?\n"$NC
printf $B"[i] "$Y"Check if you can mount umounted devices\n"$NC
cat /etc/fstab 2>/dev/null | grep -v "^#" | sed "s,$mountG,${C}[1;32m&${C}[0m,g" | sed "s,$notmounted,${C}[1;31m&${C}[0m," | sed "s,$mounted,${C}[1;34m&${C}[0m," | sed "s,$Wfolders,${C}[1;31m&${C}[0m," | sed "s,$mountpermsB,${C}[1;31m&${C}[0m,g" | sed "s,$mountpermsG,${C}[1;32m&${C}[0m,g"
echo ""
echo ""


###########################################
#---------) Available Software (----------#
###########################################
printf $B"====================================( "$GREEN"Available Software"$B" )====================================\n"$NC

#-- 1AS) Useful software
printf $Y"[+] "$GREEN"Useful software?\n"$NC
which nc ncat netcat nc.traditional wget curl ping gcc g++ make gdb base64 socat python python2 python3 python2.7 python2.6 python3.6 python3.7 perl php ruby xterm doas sudo fetch 2>/dev/null
echo ""

#-- 2AS) Search for compilers
printf $Y"[+] "$GREEN"Installed compilers?\n"$NC
(dpkg --list 2>/dev/null | grep compiler | grep -v "decompiler\|lib" 2>/dev/null || yum list installed 'gcc*' 2>/dev/null | grep gcc 2>/dev/null; which gcc g++ 2>/dev/null || locate -r "/gcc[0-9\.-]\+$" 2>/dev/null | grep -v "/doc/") || echo_not_found "Compilers"; 
echo ""
echo ""


###########################################
#-----) Processes & Cron & Services (-----#
###########################################
printf $B"================================( "$GREEN"Processes, Cron & Services"$B" )================================\n"$NC

#-- 1PCS) Cleaned proccesses
printf $Y"[+] "$GREEN"Cleaned processes\n"$NC
printf $B"[i] "$Y"Check weird & unexpected proceses run by root: https://book.hacktricks.xyz/linux-unix/privilege-escalation#processes\n"$NC
ps aux 2>/dev/null | grep -v "\[" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$rootcommon,${C}[1;32m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
echo ""

#-- 2PCS) Binary processes permissions
printf $Y"[+] "$GREEN"Binary processes permissions\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#processes\n"$NC
ps aux 2>/dev/null | awk '{print $11}'|xargs -r ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null | sed "s,$sh_usrs,${C}[1;31m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;31m&${C}[0m," | sed "s,root,${C}[1;32m&${C}[0m,"
echo ""

#-- 3PCS) Different processes 1 min
if ! [ "$FAST" ] && ! [ "$SUPERFAST" ]; then
  printf $Y"[+] "$GREEN"Different processes executed during 1 min (interesting is low number of repetitions)\n"$NC
  printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#frequent-cron-jobs\n"$NC
  if [ "`ps -e --format cmd 2>/dev/null`" ]; then for i in $(seq 1 1250); do ps -e --format cmd >> $file.tmp1; sleep 0.05; done; sort $file.tmp1 | uniq -c | grep -v "\[" | sed '/^.\{200\}./d' | sort | grep -E -v "\s*[1-9][0-9][0-9][0-9]"; rm $file.tmp1; fi
  echo ""
fi

#-- 4PCS) Cron
printf $Y"[+] "$GREEN"Cron jobs\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#scheduled-jobs\n"$NC
crontab -l 2>/dev/null | sed "s,$Wfolders,${C}[1;31;103m&${C}[0m,g" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
ls -al /etc/cron* 2>/dev/null
cat /etc/cron* /etc/at* /etc/anacrontab /var/spool/cron/crontabs/root /var/spool/anacron 2>/dev/null | grep -v "^#\|test \-x /usr/sbin/anacron\|run\-parts \-\-report /etc/cron.hourly\| root run-parts /etc/cron." | sed "s,$Wfolders,${C}[1;31;103m&${C}[0m,g" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m,"  | sed "s,root,${C}[1;31m&${C}[0m,"
crontab -l -u $USER 2>/dev/null
echo ""

#-- 5PSC) Services
printf $Y"[+] "$GREEN"Services\n"$NC
printf $B"[i] "$Y"Search for outdated versions\n"$NC
(service --status-all || chkconfig --list || rc-status) 2>/dev/null || echo_not_found "service|chkconfig|rc-status" 
echo ""
echo ""

###########################################
#---------) Network Information (---------#
###########################################
printf $B"===================================( "$GREEN"Network Information"$B" )====================================\n"$NC

#-- 1NI) Hostname, hosts and DNS
printf $Y"[+] "$GREEN"Hostname, hosts and DNS\n"$NC
cat /etc/hostname /etc/hosts /etc/resolv.conf 2>/dev/null | grep -v "^#"
dnsdomainname 2>/dev/null
echo ""

#-- 2NI) /etc/inetd.conf
printf $Y"[+] "$GREEN"Content of /etc/inetd.conf\n"$NC
(cat /etc/inetd.conf 2>/dev/null | grep -v "^#") || echo_not_found "/etc/inetd.conf" 
echo ""

#-- 3NI) Networks and neighbours
printf $Y"[+] "$GREEN"Networks and neighbours\n"$NC
cat /etc/networks 2>/dev/null
(ifconfig || ip a) 2>/dev/null
ip n 2>/dev/null
route -n 2>/dev/null
echo ""

#-- 4NI) Iptables
printf $Y"[+] "$GREEN"Iptables rules\n"$NC
(iptables -L ; cat /etc/iptables/* | grep -v "^#") 2>/dev/null || echo_not_found "iptables rules"
echo ""

#-- 5NI) Ports
printf $Y"[+] "$GREEN"Active Ports\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#internal-open-ports\n"$NC
(netstat -punta || ss -t; ss -u) 2>/dev/null | sed "s,127.0.0.1,${C}[1;31m&${C}[0m,"
echo ""

#-- 6NI) tcpdump
printf $Y"[+] "$GREEN"Can I sniff with tcpdump?\n"$NC
tcpd=`timeout 1 tcpdump 2>/dev/null`
if [ "$tcpd" ]; then
    printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#sniffing\n"$NC
    echo "You can sniff with tcpdump!" | sed "s,.*,${C}[1;31m&${C}[0m,"
else echo_no
fi
echo ""
echo ""

###########################################
#----------) Users Information (----------#
###########################################
printf $B"====================================( "$GREEN"Users Information"$B" )=====================================\n"$NC

#-- 1UI) My user
printf $Y"[+] "$GREEN"My user\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#groups\n"$NC
(id || (whoami && groups)) 2>/dev/null | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m,g" | sed "s,$knw_grps,${C}[1;32m&${C}[0m,g" | sed "s,$groupsB,${C}[1;31m&${C}[0m,g" | sed "s,$groupsVB,${C}[1;31;103m&${C}[0m,g" | sed "s,$USER,${C}[1;95m&${C}[0m,g"
echo ""

#-- 2UI) PGP keys?
printf $Y"[+] "$GREEN"Do I have PGP keys?\n"$NC
gpg --list-keys 2>/dev/null || echo_not_found "gpg"
echo ""

#-- 3UI) Sudo -l
printf $Y"[+] "$GREEN"Testing 'sudo -l' without password & /etc/sudoers\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#commands-with-sudo-and-suid-commands\n"$NC
(echo '' | sudo -S -l 2>/dev/null | sed "s,_proxy,${C}[1;31m&${C}[0m,g" | sed "s,$sudoB,${C}[1;31m&${C}[0m,g" | sed "s,$sudoVB,${C}[1;31;103m&${C}[0m,") || echo_not_found "sudo" 
(cat /etc/sudoers 2>/dev/null | sed "s,_proxy,${C}[1;31m&${C}[0m,g" | sed "s,$sudoB,${C}[1;31m&${C}[0m,g" | sed "s,$sudoVB,${C}[1;31;103m&${C}[0m,") || echo_not_found "/etc/sudoers" 
echo ""

#-- 4UI) Doas
printf $Y"[+] "$GREEN"Checking /etc/doas.conf\n"$NC
if [ "`cat /etc/doas.conf 2>/dev/null`" ]; then cat /etc/doas.conf 2>/dev/null | sed "s,$sh_usrs,${C}[1;31m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m," | sed "s,nopass,${C}[1;31m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$USER,${C}[1;31;103m&${C}[0m,"
else echo_not_found "/etc/doas.conf"
fi
echo ""

#-- 5UI) Pkexec policy
printf $Y"[+] "$GREEN"Checking Pkexec policy\n"$NC
(cat /etc/polkit-1/localauthority.conf.d/* 2>/dev/null | grep -v "^#" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$groupsB,${C}[1;31m&${C}[0m," | sed "s,$groupsVB,${C}[1;31m&${C}[0m," | sed "s,$USER,${C}[1;31;103m&${C}[0m," | sed "s,$GROUPS,${C}[1;31;103m&${C}[0m,") || echo_not_found "/etc/polkit-1/localauthority.conf.d"
echo ""

#-- 6UI) Brute su
if [ "$TIMEOUT" ]; then
  printf $Y"[+] "$GREEN"Testing 'su' as other users with shell without password or with their names as password (only works in modern su binary versions)\n"$NC
  SHELLUSERS=`cat /etc/passwd 2>/dev/null | grep -i "sh$" | cut -d ":" -f 1`
  for u in $SHELLUSERS; do
    echo "Trying with $u..."
    trysu=`echo "" | timeout 1 su $u -c whoami 2>/dev/null`
    if [ "$trysu" ]; then
      echo "You can login as $u whithout password!" | sed "s,.*,${C}[1;31m&${C}[0m,"
    else
      trysu=`echo $u | timeout 1 su $u -c whoami 2>/dev/null`
      if [ "$trysu" ]; then
        echo "You can login as $u using the username as password!" | sed "s,.*,${C}[1;31m&${C}[0m,"
      fi
    fi
  done
else
  printf $Y"[+] "$GREEN"Don forget to test 'su' as any other user with shell: without password and with their names as password (I can't do it...)\n"$NC
fi
printf $Y"[+] "$GREEN"Do not forget to execute 'sudo -l' without password or with valid password (if you know it)!!\n"$NC
echo ""

#-- 7UI) Superusers
printf $Y"[+] "$GREEN"Superusers\n"$NC
awk -F: '($3 == "0") {print}' /etc/passwd 2>/dev/null | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;31;103m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
echo ""

#-- 8UI) Users with console
printf $Y"[+] "$GREEN"Users with console\n"$NC
cat /etc/passwd 2>/dev/null | grep "sh$" | sort | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
echo ""

#-- 9UI) Login info
printf $Y"[+] "$GREEN"Login information\n"$NC
w 2>/dev/null | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
last 2>/dev/null | tail | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
echo ""

#-- 10UI) All users
printf $Y"[+] "$GREEN"All users\n"$NC
cat /etc/passwd 2>/dev/null | sort | cut -d: -f1 | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m,g" | sed "s,root,${C}[1;31m&${C}[0m,"
echo ""
echo ""

###########################################
#--------) Software Information (---------#
###########################################
printf $B"===================================( "$GREEN"Software Information"$B" )===================================\n"$NC

#-- 1SI) Mysql version
printf $Y"[+] "$GREEN"MySQL version\n"$NC
mysql --version 2>/dev/null || echo_not_found "mysql"
echo ""

#-- 2SI) Mysql connection root/root
printf $Y"[+] "$GREEN"MySQL connection using default root/root ........... "$NC
mysqlconnect=`mysqladmin -uroot -proot version 2>/dev/null`
if [ "$mysqlconnect" ]; then
  echo "Yes" | sed "s,.*,${C}[1;31m&${C}[0m,"
  mysql -u root --password=root -e "SELECT User,Host,authentication_string FROM mysql.user;" 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
else echo_no
fi

#-- 3SI) Mysql connection root/toor
printf $Y"[+] "$GREEN"MySQL connection using root/toor ................... "$NC
mysqlconnect=`mysqladmin -uroot -ptoor version 2>/dev/null`
if [ "$mysqlconnect" ]; then
  echo "Yes" | sed "s,.*,${C}[1;31m&${C}[0m,"
  mysql -u root --password=toor -e "SELECT User,Host,authentication_string FROM mysql.user;" 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
else echo_no
fi

#-- 4SI) Mysql connection root/NOPASS
mysqlconnectnopass=`mysqladmin -uroot version 2>/dev/null`
printf $Y"[+] "$GREEN"MySQL connection using root/NOPASS ................. "$NC
if [ "$mysqlconnectnopass" ]; then
  echo "Yes" | sed "s,.*,${C}[1;31m&${C}[0m,"
  mysql -u root -e "SELECT User,Host,authentication_string FROM mysql.user;" 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
else echo_no
fi

#-- 5SI) Mysql credentials
printf $Y"[+] "$GREEN"Looking for mysql credentials\n"$NC
mysqldirs=`find /etc /usr/var/lib /var/lib -type d -name mysql -not -path "*mysql/mysql"  2>/dev/null`
if [ "$mysqldirs" ]; then
  for d in $mysqldirs; do 
    dcnf=`find $d -name debian.cnf 2>/dev/null`
    for f in $dcnf; do
      if [ -r $f ]; then 
        echo "We can read the mysql debian.cnf. You can use this username/password to log in MySQL" | sed "s,.*,${C}[1;31m&${C}[0m,"
        cat $f 
      fi
    done
    uMYD=`find $d -name user.MYD 2>/dev/null`
    for f in $uMYD; do
      if [ -r $f ]; then 
        echo "We can read the Mysql Hashes from $f" | sed "s,.*,${C}[1;31m&${C}[0m,"
        grep -oaE "[-_\.\*a-Z0-9]{3,}" $f | grep -v "mysql_native_password" 
      fi
    done
    user=`grep -lr "user\s*=" $d 2>/dev/null | grep -v "debian.cnf"`
    for f in $user; do
      if [ -r $f ]; then
        u=`cat $f | grep -v "#" | grep "user" | grep "=" 2>/dev/null`
        echo "From '$f' Mysql user: $u" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
      fi
    done
  done
else echo_not_found
fi
echo ""

#-- 6SI) PostgreSQL info
printf $Y"[+] "$GREEN"PostgreSQL version and pgadmin credentials\n"$NC
postgver=`psql -V 2>/dev/null`
postgdb=`find /var /etc /home /root /tmp /usr /opt -type f -name "pgadmin*.db" 2>/dev/null`
if [ "$postgver" ] || [ "$postgdb"]; then
  if [ "$postgver" ]; then echo "Version: $postgver"; fi
  if [ "$postgdb" ]; then echo "PostgreSQL database: $postgdb" | sed "s,.*,${C}[1;31m&${C}[0m,"; fi
else echo_not_found
fi
echo ""

#-- 7SI) PostgreSQL brute
if [ "$TIMEOUT" ]; then  # In some OS (like OpenBSD) it will expect the password from console and will pause the script. Also, this OS doesn't have the "timeout" command so lets only use this checks in OS that has it.
#checks to see if any postgres password exists and connects to DB 'template0' - following commands are a variant on this
  printf $Y"[+] "$GREEN"PostgreSQL connection to template0 using postgres/NOPASS ........ "$NC
  if [ "`timeout 1 psql -U postgres -d template0 -c 'select version()' 2>/dev/null`" ]; then echo "Yes" | sed "s,.*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi

  printf $Y"[+] "$GREEN"PostgreSQL connection to template1 using postgres/NOPASS ........ "$NC
  if [ "`timeout 1 psql -U postgres -d template1 -c 'select version()' 2>/dev/null`" ]; then echo "Yes" | sed "s,.)*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi

  printf $Y"[+] "$GREEN"PostgreSQL connection to template0 using pgsql/NOPASS ........... "$NC
  if [ "`timeout 1 psql -U pgsql -d template0 -c 'select version()' 2>/dev/null`" ]; then echo "Yes" | sed "s,.*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi

  printf $Y"[+] "$GREEN"PostgreSQL connection to template1 using pgsql/NOPASS ........... "$NC
  if [ "`timeout 1 psql -U pgsql -d template1 -c 'select version()' 2> /dev/null`" ]; then echo "Yes" | sed "s,.*,${C}[1;31m&${C}[0m,"
  else echo_no
  fi
  echo ""
fi

#-- 8SI) Apache info
printf $Y"[+] "$GREEN"Apache server info\n"$NC
apachever=`apache2 -v 2>/dev/null; httpd -v 2>/dev/null`
if [ "$apachever" ]; then
  echo "Version: $apachever"
  sitesenabled=`find /var /etc /home /root /tmp /usr /opt -name sites-enabled -type d 2>/dev/null`
  for d in $sitesenabled; do for f in $d/*; do grep "AuthType\|AuthName\|AuthUserFile" $f 2>/dev/null | sed "s,.*AuthUserFile.*,${C}[1;31m&${C}[0m,"; done; done
  if [ !"$sitesenabled" ]; then
    default00=`find /var /etc /home /root /tmp /usr /opt -name 000-default 2>/dev/null`
    for f in $default00; do grep "AuthType\|AuthName\|AuthUserFile" $f 2>/dev/null | sed "s,.*AuthUserFile.*,${C}[1;31m&${C}[0m,"; done
  fi
else echo_not_found
fi
echo ""

#-- 9SI) PHP cookies files
phpsess1=`ls /var/lib/php/sessions 2>/dev/null`
phpsess2=`find /tmp /var/tmp -name "sess_*" 2>/dev/null`
printf $Y"[+] "$GREEN"Looking for PHPCookies\n"$NC
if [ "$phpsess1" ] || [ "$phpsess2" ]; then
  if [ "$phpsess1" ]; then ls /var/lib/php/sessions 2>/dev/null; fi
  if [ "$phpsess2" ]; then find /tmp /var/tmp -name "sess_*" 2>/dev/null; fi
else echo_not_found
fi
echo ""

#-- 10SI) Wordpress user, password, databname and host
printf $Y"[+] "$GREEN"Looking for Wordpress wp-config.php files\n"$NC
wp=`find /var /etc /home /root /tmp /usr /opt -type f -name wp-config.php 2>/dev/null`
if [ "$wp" ]; then
  echo "wp-config.php files found:\n$wp"
  for f in $wp; do grep "PASSWORD\|USER\|NAME\|HOST" $f 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"; done
else echo_not_found "wp-config.php"
fi
echo ""

#-- 11SI) Tomcat users
printf $Y"[+] "$GREEN"Looking for Tomcat users file\n"$NC
wp=`find /var /etc /home /root /tmp /usr /opt -type f -name tomcat-users.xml 2>/dev/null`
if [ "$wp" ]; then
  echo "tomcat-users.xml file found: $wp"
  for f in $wp; do grep "username=" $f 2>/dev/null | grep "password=" | sed "s,.*,${C}[1;31m&${C}[0m,"; done
else echo_not_found "tomcat-users.xml"
fi
echo ""

#-- 12SI) Mongo Information
printf $Y"[+] "$GREEN"Mongo information\n"$NC
(mongo --version 2>/dev/null || mongod --version 2>/dev/null) || echo_not_found 
#TODO: Check if you can login without password and warn the user
echo ""

#-- 13SI) Supervisord conf file
printf $Y"[+] "$GREEN"Looking for supervisord configuration file\n"$NC
supervisor=`find /var /etc /home /root /tmp /usr /opt -name supervisord.conf 2>/dev/null`
if [ "$supervisor" ]; then
  echo $supervisor
  for f in $supervisor; do cat $f 2>/dev/null | grep "port.*=\|username.*=\|password=.*" | sed "s,port\|username\|password,${C}[1;31m&${C}[0m,"; done
else echo_not_found "supervisord.conf"
fi
echo ""

#-- 14SI) Cesi conf file
cesi=`find /var /etc /home /root /tmp /usr /opt -name cesi.conf 2>/dev/null`
printf $Y"[+] "$GREEN"Looking for cesi configuration file\n"$NC
if [ "$cesi" ]; then
  echo $cesi
  for f in $cesi; do cat $f 2>/dev/null | grep "username.*=\|password.*=\|host.*=\|port.*=\|database.*=" | sed "s,username\|password\|database,${C}[1;31m&${C}[0m,"; done
else echo_not_found "cesi.conf"
fi
echo ""

#-- 15SI) Rsyncd conf file
rsyncd=`find /var /etc /home /root /tmp /usr /opt -name rsyncd.conf 2>/dev/null`
printf $Y"[+] "$GREEN"Looking for Rsyncd config file\n"$NC
if [ "$rsyncd" ]; then
  echo $rsyncd
  for f in $rsyncd; do cat $f 2>/dev/null | grep -v "^#" | grep "uid.*=|\gid.*=\|path.*=\|auth.*users.*=\|secrets.*file.*=\|hosts.*allow.*=\|hosts.*deny.*=" | sed "s,secrets.*,${C}[1;31m&${C}[0m,"; done
else echo_not_found "rsyncd.conf"
fi
echo ""

##-- 16SI) Hostapd conf file
printf $Y"[+] "$GREEN"Looking for Hostapd config file\n"$NC
hostapd=`find /var /etc /home /root /tmp /usr /opt -name hostapd.conf 2>/dev/null`
if [ "$hostapd" ]; then
  printf $Y"[+] "$GREEN"Hostapd conf was found\n"$NC
  echo $hostapd
  for f in $hostapd; do cat $f 2>/dev/null | grep "passphrase" | sed "s,passphrase.*,${C}[1;31m&${C}[0m,"; done
else echo_not_found "hostapd.conf"
fi
echo ""

##-- 17SI) Wifi conns
printf $Y"[+] "$GREEN"Looking for wifi conns file\n"$NC
wifi=`find /etc/NetworkManager/system-connections/ 2>/dev/null`
if [ "$wifi" ]; then
  echo $wifi
  for f in $wifi; do cat $f 2>/dev/null | grep "psk.*=" | sed "s,psk.*,${C}[1;31m&${C}[0m,"; done
else echo_not_found
fi
echo ""

##-- 18SI) Anaconda-ks conf files
printf $Y"[+] "$GREEN"Looking for Anaconda-ks config files\n"$NC
anaconda=`find /var /etc /home /root /tmp /usr /opt -name anaconda-ks.cfg 2>/dev/null`
if [ "$anaconda" ]; then
  echo $anaconda
  for f in $anaconda; do cat $f 2>/dev/null | grep "rootpw" | sed "s,rootpw.*,${C}[1;31m&${C}[0m,"; done
else echo_not_found "anaconda-ks.cfg"
fi
echo ""

##-- 19SI) VNC files
printf $Y"[+] "$GREEN"Looking for .vnc directories and their passwd files\n"$NC
vnc=`find /home /root -type d -name .vnc 2>/dev/null`
if [ "$vnc" ]; then
  echo $vnc
  for d in $vnc; do find $d -name "passwd" -exec ls -l {} \; 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"; done
else echo_not_found ".vnc"
fi
echo ""

##-- 20SI) LDAP directories
printf $Y"[+] "$GREEN"Looking for ldap directories and their hashes\n"$NC
ldap=`find /var /etc /home /root /tmp /usr /opt -type d -name ldap 2>/dev/null`
if [ "$ldap" ]; then
  echo $ldap
  echo "The password hash is from the {SSHA} to 'structural'";
  for d in $ldap; do cat $d/*.bdb 2>/dev/null | grep -i -a -E -o "description.*" | sort | uniq | sed "s,administrator\|password\|ADMINISTRATOR\|PASSWORD\|Password\|Administrator,${C}[1;31m&${C}[0m,g"; done
else echo_not_found ".vnc"
fi
echo ""

##-- 21SI) .ovpn files
printf $Y"[+] "$GREEN"Looking for .ovpn files and credentials\n"$NC
ovpn=`find /etc /usr /home /root -name .ovpn 2>/dev/null`
if [ "$ovpn" ]; then
  echo $ovpn
  for f in $ovpn; do cat $f 2>/dev/null | grep "auth-user-pass" | sed "s,auth-user-pass.*,${C}[1;31m&${C}[0m,"; done
else echo_not_found ".ovpn"
fi
echo ""

##-- 22SI) ssh files
printf $Y"[+] "$GREEN"Looking for ssl/ssh files\n"$NC
ssh=`find /home /usr /root /etc /opt /var /mnt \( -name "id_dsa*" -o -name "id_rsa*" -o -name "known_hosts" -o -name "authorized_hosts" -o -name "authorized_keys" \) 2>/dev/null`
privatekeyfiles=`grep -rl "PRIVATE KEY-----" /home /root /mnt /etc 2>/dev/null`
certsb4=`find /home /usr /root /etc /opt /var /mnt \( -name "*.pem" -o -name "*.cer" -o -name "*.crt" \) 2>/dev/null | grep -v "/usr/share/\|/etc/ssl/"`
certsbin=`find /home /usr /root /etc /opt /var /mnt \( -name "*.csr" -o -name "*.der" \) 2>/dev/null | grep -v "/usr/share/\|/etc/ssl/"`
clientcert=`find /home /usr /root /etc /opt /var /mnt \( -name "*.pfx" -o -name "*.p12" \) 2>/dev/null | grep -v "/usr/share/\|/etc/ssl/"`

if [ "$ssh"  ]; then
  echo $ssh
fi

grep "PermitRootLogin \|ChallengeResponseAuthentication \|PasswordAuthentication \|UsePAM \|Port\|PermitEmptyPasswords\|PubkeyAuthentication\|ListenAddress" /etc/ssh/sshd_config 2>/dev/null | grep -v "#" | sed "s,PermitRootLogin.*es\|PermitEmptyPasswords.*es\|ChallengeResponseAuthentication.*es,${C}[1;31m&${C}[0m,"

if [ "$privatekeyfiles" ]; then
  privatekeyfilesgrep=`grep -L "\"\|'\|(" $privatekeyfiles` # Check there aren't unexpected symbols in the file
fi
if [ "$privatekeyfilesgrep" ]; then
  printf "Private SSH keys found!:\n$privatekeyfilesgrep\n" | sed "s,.*,${C}[1;31m&${C}[0m,"
fi
if [ "$certsb4" ] || [ "$certsbin" ]; then
  echo "  -- Some certificates were found:"
  grep -L "\"\|'\|(" $certsb4 2>/dev/null
  echo $certsbin
fi
if [ "$clientcert" ]; then
  echo "  -- Some client certificates were found:"
  echo $clientcert
fi
echo ""

##-- 23SI) PAM auth
printf $Y"[+] "$GREEN"Looking for unexpected auth lines in /etc/pam.d/sshd\n"$NC
pamssh=`cat /etc/pam.d/sshd 2>/dev/null | grep -v "^#\|^@" | grep -i auth`
if [ "$pamssh" ]; then
  cat /etc/pam.d/sshd 2>/dev/null | grep -v "^#\|^@" | grep -i auth | sed "s,.*,${C}[1;31m&${C}[0m,"
else echo_no
fi
echo ""

if ! [ "$SUPERFAST" ]; then
  ##-- 24SI) AWS keys files
  printf $Y"[+] "$GREEN"Looking for AWS Keys\n"$NC
  (grep -rli "aws_secret_access_key" /home /root /mnt /etc 2>/dev/null | grep -v $(basename "$0" 2>/dev/null) | sed "s,.*,${C}[1;31m&${C}[0m,") || echo_not_found
  echo ""
fi

##-- 25SI) NFS exports
printf $Y"[+] "$GREEN"NFS exports?\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation/nfs-no_root_squash-misconfiguration-pe\n"$NC
if [ "`cat /etc/exports 2>/dev/null`" ]; then cat /etc/exports 2>/dev/null | grep -v "^#" | sed "s,no_root_squash\|no_all_squash ,${C}[1;31;103m&${C}[0m,"
else echo_not_found "/etc/exports"
fi
echo ""

##-- 26SI) Kerberos
printf $Y"[+] "$GREEN"Looking for kerberos conf files and tickets\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/pentesting/pentesting-kerberos-88#pass-the-ticket-ptt\n"$NC
krb5=`find /var /etc /home /root /tmp /usr /opt -type d -name krb5.conf 2>/dev/null`
if [ "$krb5" ]; then
  for f in $krb5; do cat /etc/krb5.conf | grep default_ccache_name | sed "s,default_ccache_name,${C}[1;31m&${C}[0m,"; done
else echo_not_found "krb5.conf"
fi
ls -l "/tmp/krb5cc*" "/var/lib/sss/db/ccache_*" "/etc/opt/quest/vas/host.keytab" 2>/dev/null || echo_not_found "tickets kerberos"
echo ""

##-- 27SI) kibana
printf $Y"[+] "$GREEN"Looking for Kibana yaml\n"$NC
kibana=`find /var /etc /home /root /tmp /usr /opt -name "kibana.y*ml" 2>/dev/null`
if [ "$kibana" ]; then
  echo $kibana
  for f in $kibana; do cat $f 2>/dev/null || grep -v "^#" | grep -v -e '^[[:space:]]*$' | sed "s,username\|password\|host\|port\|elasticsearch\|ssl,${C}[1;31m&${C}[0m,"; done
else echo_not_found "kibana.yml"
fi
echo ""

###-- 28SI) Logstash
printf $Y"[+] "$GREEN"Looking for logstash files\n"$NC
logstash=`find /var /etc /home /root /tmp /usr /opt -type d -name logstash 2>/dev/null`
if [ "$logstash" ]; then
  echo $logstash
  for d in $logstash; do
    if [ -r $d/startup.options ]; then 
      echo "Logstash is running as user:"
      cat $d/startup.options 2>/dev/null | grep "LS_USER\|LS_GROUP" | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$nosh_usrs,${C}[1;34m&${C}[0m," | sed "s,$knw_usrs,${C}[1;32m&${C}[0m," | sed "s,$USER,${C}[1;95m&${C}[0m," | sed "s,root,${C}[1;31m&${C}[0m,"
    fi
    cat $d/conf.d/out* | grep "exec\s*{\|command\s*=>" | sed "s,exec\s*{\|command\s*=>,${C}[1;31m&${C}[0m,"
    cat $d/conf.d/filt* | grep "path\s*=>\|code\s*=>\|ruby\s*{" | sed "s,path\s*=>\|code\s*=>\|ruby\s*{,${C}[1;31m&${C}[0m,"
  done
else echo_not_found
fi
echo ""

##-- 29SI) Elasticsearch
printf $Y"[+] "$GREEN"Looking for elasticsearch files\n"$NC
elasticsearch=`find /var /etc /home /root /tmp /usr /opt -name "elasticsearch.y*ml" 2>/dev/null`
if [ "$elasticsearch" ]; then
  echo $elasticsearch
  for f in $elasticsearch; do cat $f 2>/dev/null | grep -v "^#" | grep -v -e '^[[:space:]]*$' | grep "path.data\|path.logs\|cluster.name\|node.name\|network.host\|discovery.zen.ping.unicast.hosts"; done
  echo "Version: $(curl -X GET '10.10.10.115:9200' 2>/dev/null | grep number | cut -d ':' -f 2)"
else echo_not_found
fi
echo ""

##-- 30SI) Vault-ssh
printf $Y"[+] "$GREEN"Looking for Vault-ssh files\n"$NC
vaultssh=`find /etc /usr /home /root -name vault-ssh-helper.hcl 2>/dev/null`
if [ "$vaultssh" ]; then
  echo $vaultssh
  for f in $vaultssh; do cat $f 2>/dev/null; vault-ssh-helper -verify-only -config $f 2>/dev/null; done
  echo ""
  vault secrets list 2>/dev/null
  find /etc /usr /home /root -name ".vault-token" 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m," 2>/dev/null
else echo_not_found "vault-ssh-helper.hcl"
fi
echo ""

##-- 31SI) Cached AD Hashes
adhashes= `ls "/var/lib/samba/private/secrets.tdb" "/var/lib/samba/passdb.tdb" "/var/opt/quest/vas/authcache/vas_auth.vdb" "/var/lib/sss/db/cache_*" 2>/dev/null`
printf $Y"[+] "$GREEN"Looking for AD cached hahses\n"$NC
if [ "$adhashes" ]; then
  ls "/var/lib/samba/private/secrets.tdb" "/var/lib/samba/passdb.tdb" "/var/opt/quest/vas/authcache/vas_auth.vdb" "/var/lib/sss/db/cache_*" 2>/dev/null
else echo_not_found "cached hashes"
fi
echo ""

###########################################
#----------) Interesting files (----------#
###########################################
printf $B"====================================( "$GREEN"Interesting Files"$B" )=====================================\n"$NC

##-- 1IF) SUID
printf $Y"[+] "$GREEN"SUID\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#commands-with-sudo-and-suid-commands\n"$NC
for s in `find / -perm -4000 2>/dev/null`; do
  c="a"
  for b in $sidB; do
    if [ "`echo $s | grep $(echo $b | cut -d "%" -f 1)`" ]; then
      echo $s | sed "s,$(echo $b | cut -d "%" -f 1),${C}[1;31m&\t\t--->\t$(echo $b | cut -d "%" -f 2)${C}[0m,"
      c=""
      break;
    fi
  done;
  if [ "$c" ]; then
      echo $s | sed "s,$sidG,${C}[1;32m&${C}[0m," | sed "s,$sidVB,${C}[1;31;103m&${C}[0m,"
    fi
done;
echo ""

##-- 2IF) SGID
printf $Y"[+] "$GREEN"SGID\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#commands-with-sudo-and-suid-commands\n"$NC
for s in `find / -perm -g=s -type f 2>/dev/null`; do
  c="a"
  for b in $sidB; do
    if [ "`echo $s | grep $(echo $b | cut -d "%" -f 1)`" ]; then
      echo $s | sed "s,$(echo $b | cut -d "%" -f 1),${C}[1;31m&\t\t--->\t$(echo $b | cut -d "%" -f 2)${C}[0m,"
      c=""
      break;
    fi
  done;
  if [ "$c" ]; then
      echo $s | sed "s,$sidG,${C}[1;32m&${C}[0m," | sed "s,$sidVB,${C}[1;31;103m&${C}[0m,"
    fi
done;
echo ""

##-- 3IF) Capabilities
printf $Y"[+] "$GREEN"Capabilities\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#capabilities\n"$NC
(getcap -r / 2>/dev/null | sed "s,$sudocapsB,${C}[1;31m&${C}[0m," | sed "s,$capsB,${C}[1;31m&${C}[0m,") || echo_not_found
echo ""

##-- 4IF) .sh files in PATH
printf $Y"[+] "$GREEN".sh files in path\n"$NC
for d in `echo $PATH | tr ":" "\n"`; do find $d -name "*.sh" 2>/dev/null | sed "s,$pathshG,${C}[1;32m&${C}[0m," ; done
echo ""

##-- 5IF) Hashes in passwd file
printf $Y"[+] "$GREEN"Hashes inside passwd file? ........... "$NC
if [ "`grep -v '^[^:]*:[x\*]' /etc/passwd 2>/dev/null`" ]; then grep -v '^[^:]*:[x\*]' /etc/passwd 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
else echo_no
fi

##-- 6IF) Read shadow files
printf $Y"[+] "$GREEN"Can I read shadow files? ........... "$NC
if [ "`cat /etc/shadow /etc/master.passwd 2>/dev/null`" ]; then cat /etc/shadow /etc/master.passwd 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"
else echo_no
fi

##-- 7IF) Read root dir
printf $Y"[+] "$GREEN"Can I read root folder? ........... "$NC
(ls -ahl /root/ 2>/dev/null) || echo_no
echo ""

##-- 8IF) Root files in home dirs
printf $Y"[+] "$GREEN"Looking for root files in home dirs (limit 20)\n"$NC
(find /home -user root 2>/dev/null | head -n 20 | sed "s,$sh_usrs,${C}[1;96m&${C}[0m," | sed "s,$USER,${C}[1;31m&${C}[0m,") || echo_not_found
echo ""

##-- 9IF) Root files in my dirs
printf $Y"[+] "$GREEN"Looking for root files in folders owned by me\n"$NC
(for d in `find /var /etc /home /root /tmp /usr /opt /boot /sys -type d -user $USER 2>/dev/null`; do find $d -user root -exec ls -l {} \; 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m," ; done) || echo_not_found
echo ""

##-- 10IF) Readable files belonging to root and not world readable
printf $Y"[+] "$GREEN"Readable files belonging to root and readable by me but not world readable\n"$NC
(for f in `find / -type f -user root ! -perm -o=r 2>/dev/null`; do if [ -r $f ]; then ls -l $f 2>/dev/null | sed "s,.*,${C}[1;31m&${C}[0m,"; fi; done) || echo_not_found
echo ""

##-- 11IF) Files inside my home
printf $Y"[+] "$GREEN"Files inside $HOME (limit 20)\n"$NC
(ls -la $HOME 2>/dev/null | head -n 23) || echo_not_found
echo ""

##-- 12IF) Files inside /home
printf $Y"[+] "$GREEN"Files inside others home (limit 20)\n"$NC
(find /home -type f 2>/dev/null | grep -v -i "/"$USER | head -n 20) || echo_not_found
echo ""

##-- 13IF) Mails
printf $Y"[+] "$GREEN"Mails (limited 50)\n"$NC
(find /var/mail/ /var/spool/mail/ -type f 2>/dev/null | head -n 50) || echo_not_found
echo ""

##-- 14IF) Backup files
printf $Y"[+] "$GREEN"Backup files?\n"$NC
backs=`find /var /etc /bin /sbin /home /usr/local/bin /usr/local/sbin /usr/bin /usr/games /usr/sbin /root /tmp -type f \( -name "*backup*" -o -name "*\.bak" -o -name "*\.bck" -o -name "*\.bk" \) 2>/dev/null` 
for b in $backs; do if [ -r $b ]; then ls -l $b | grep -v $notBackup | sed "s,backup\|bck\|\.bak,${C}[1;31m&${C}[0m,"; fi; done
echo ""

##-- 15IF) DB files
printf $Y"[+] "$GREEN"Looking for readable .db files\n"$NC
dbfiles=`find /var /etc /home /root /tmp /usr /opt -type f -name "*.db" 2>/dev/null`
for f in $dbfiles; do if [ -r $f ]; then echo $f; fi; done
echo ""

##-- 16IF) Web files
printf $Y"[+] "$GREEN"Web files?(output limited)\n"$NC
ls -alhR /var/www/ 2>/dev/null | head
ls -alhR /srv/www/htdocs/ 2>/dev/null | head
ls -alhR /usr/local/www/apache22/data/ 2>/dev/null | head
ls -alhR /opt/lampp/htdocs/ 2>/dev/null | head
echo ""

##-- 17IF) Interesting hidden files
printf $Y"[+] "$GREEN"*_history, .sudo_as_admin_successful, profile, bashrc, httpd.conf, .plan, .htpasswd, .git-credentials, .rhosts, hosts.equiv, Dockerfile, docker-compose.yml\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#read-sensitive-data\n"$NC
fils=`find / -type f \( -name "*_history" -o -name ".sudo_as_admin_successful" -o -name ".profile" -o -name "*bashrc" -o -name "httpd.conf" -o -name "*.plan" -o -name ".htpasswd" -o -name ".git-credentials" -o -name "*.rhosts" -o -name "hosts.equiv" -o -name "Dockerfile" -o -name "docker-compose.yml" \) 2>/dev/null`
for f in $fils; do 
  if [ -r $f ]; then 
    ls -l $f 2>/dev/null | sed "s,bash_history\|\.sudo_as_admin_successful\|\.plan\|\.htpasswd\|\.git-credentials\|\.rhosts\|,${C}[1;31m&${C}[0m," | sed "s,$sh_usrs,${C}[1;96m&${C}[0m,g" | sed "s,$USER,${C}[1;95m&${C}[0m,g" | sed "s,root,${C}[1;31m&${C}[0m,g"; 
    g=`echo $f | grep "_history"`
    if [ $g ]; then
      printf $GREEN"Looking for possible passwords inside $f\n"$NC
      cat $f | grep $pwd_inside_history | sed "s,$pwd_inside_history,${C}[1;31m&${C}[0m,"
      echo ""
    fi;
  fi; 
done
echo ""

##-- 18IF) All hidden files
printf $Y"[+] "$GREEN"All hidden files (not in /sys/ or the ones listed in the previous check) (limit 100)\n"$NC
find / -type f -iname ".*" -ls 2>/dev/null | grep -v "/sys/\|\.gitignore\|_history$\|\.profile\|\.bashrc\|\.listing\|\.ignore\|\.uuid\|\.plan\|\.htpasswd\|\.git-credentials\|.rhosts\|.depend" | head -n 100
echo ""

##-- 19IF) Readable files in /tmp, /var/tmp, /var/backups
printf $Y"[+] "$GREEN"Readable files inside /tmp, /var/tmp, /var/backups(limit 100)\n"$NC
filstmpback=`find /tmp /var/tmp /var/backups -type f 2>/dev/null | head -n 100`
for f in $filstmpback; do if [ -r $f ]; then ls -l $f 2>/dev/null; fi; done
echo ""

##-- 20IF) Interesting writable files
printf $Y"[+] "$GREEN"Interesting writable Files\n"$NC
printf $B"[i] "$Y"https://book.hacktricks.xyz/linux-unix/privilege-escalation#writable-files\n"$NC
find / '(' -type f -or -type d ')' '(' '(' -user $USER ')' -or '(' -perm -o=w ')' ')' 2>/dev/null | grep -v '/proc/' | grep -v $HOME | grep -v '/sys/fs' | grep -v $notExtensions | sort | uniq | sed "s,$writeB,${C}[1;31m&${C}[0m," | sed "s,$writeVB,${C}[1;31:93m&${C}[0m,"
for g in `groups`; do find / \( -type f -or -type d \) -group $g -perm -g=w 2>/dev/null | grep -v '/proc/' | grep -v $HOME | grep -v '/sys/fs' | grep -v $notExtensions | sed "s,$writeB,${C}[1;31m&${C}[0m," | sed "s,$writeVB,${C}[1;31;103m&${C}[0m,"; done
echo ""

##-- 21IF) Passwords in config PHP files
printf $Y"[+] "$GREEN"Searching passwords in config PHP files\n"$NC
configs=`find /var /etc /home /root /tmp /usr /opt -type f -name "*config*.php" 2>/dev/null`
for c in $configs; do grep -i "password.* = ['\"]\|define.*passw\|db_pass" $c 2>/dev/null | grep -v "function\|password.* = \"\"\|password.* = ''" | sed '/^.\{150\}./d' | sort | uniq | sed "s,password\|db_pass,${C}[1;31m&${C}[0m,i"; done
echo ""

##-- 22IF) IPs inside logs
printf $Y"[+] "$GREEN"Finding IPs inside logs\n"$NC
grep -R -a -E -o "(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)" /var/log/ 2>/dev/null | sort | uniq -c
echo ""

##-- 23IF) Passwords inside logs
printf $Y"[+] "$GREEN"Finding passwords inside logs (limited 100)\n"$NC
grep -R -i "pwd\|passw" /var/log/ 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | grep -v "File does not exist:\|script not found or unable to stat:\|\"GET /.*\" 404" | head -n 100 | sed "s,pwd\|passw,${C}[1;31m&${C}[0m,"
echo ""

##-- 24IF) Emails inside logs
printf $Y"[+] "$GREEN"Finding emails inside logs (limited 100)\n"$NC
grep -R -E -o "\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,6}\b" /var/log/ 2>/dev/null | sort | uniq -c | head -n 100 
echo "" 

if ! [ "$SUPERFAST" ]; then
  ##-- 25IF) Passwords inside files
  printf $Y"[+] "$GREEN"Finding 'pwd' or 'passw' string inside /home, /var/www, /etc, /root and list possible web(/var/www) and config(/etc) passwords\n"$NC
  grep -lRi "pwd\|passw" /home /var/www /root 2>/dev/null | sort | uniq
  grep -R -i "password.* = ['\"]\|define.*passw" /var/www /root /home 2>/dev/null | grep "\.php" | grep -v "function\|password.* = \"\"\|password.* = ''" | sed '/^.\{150\}./d' | sort | uniq | sed "s,password,${C}[1;31m&${C}[0m,"
  grep -R -i "password" /etc 2>/dev/null | grep "conf" | grep -v ":#\|:/\*\|: \*" | sort | uniq | sed "s,password,${C}[1;31m&${C}[0m,"
  echo ""
fi