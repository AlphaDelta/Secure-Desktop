# Secure-Desktop

Secure Desktop is a tool for windows to open programs in a safe area where keyloggers and Remote Administration Tools cannot access by any conventional means.

## Features

 * Runs under the .NET Framework 2.0
 * Strict cleanup system terminates orphan processes often created by malicious programs after the primary process has closed
 * Desktop agent that ensures cleanup and provides in-desktop hotkeys
   * Supresses accidental PrintScreen presses in the event privacy is a concern (Hold ctrl to circumvent).
   * Ctrl + Alt + K - Emergency exit (In the event the primary process becomes indefinitely unresponsive).
   * Ctrl + Alt + V - View processes currently open in the secure desktop.
   * Ctrl + Shift + Esc - Open Task Manager in the secure desktop.

## Using Secure Desktop

#### GUI

<img src="http://i.imgur.com/oN6Icdl.png" />

Simply drop a file into the form, or select a file via File > Open, then click on the 'Run' button in the bottom left corner

#### CLI

Run `SecureDesktop.exe` with the file and arguments.

Example: `SecureDesktop.exe "C:\Users\Admin\Documents\file with spaces.exe" -some -executable -parameters`

## Limitations and faults

The way Secure Desktop works is that a new desktop is created that cannot be accessed via the WinAPI, which means mouse and keyboard hooks in the main desktop will not function inside of it.

However due to the nature of how memory is managed in windows processes are free to access and edit the memory of any other process regardless of what desktops they're on.
