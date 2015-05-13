# Secure-Desktop

#### Introduction
Secure Desktop is a tool for windows to open programs in a safe area where keyloggers and Remote Administration Tools cannot access by any conventional means

#### Limitations and faults

The way Secure Desktop works is that a new desktop is created that cannot be accessed via the WinAPI, which means mouse and keyboard hooks in the main desktop will not function inside of it.

However due to the nature of how memory is managed in windows processes are free to access and edit the memory of any other process regardless of what desktops they're on.
