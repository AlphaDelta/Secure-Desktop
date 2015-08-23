# Secure-Desktop

Secure Desktop is a tool for Windows to open programs in a safe area where keyloggers and Remote Administration Tools cannot access by any conventional means.

Software keyloggers work by setting up a [Windows hook](https://msdn.microsoft.com/en-us/library/windows/desktop/ms644985.aspx) that tells Windows that whenever the user [presses a key](https://msdn.microsoft.com/en-us/library/windows/desktop/ms644959.aspx#wh_keyboard_llhook) or [uses their mouse](https://msdn.microsoft.com/en-us/library/windows/desktop/ms644959.aspx#wh_mouse_llhook) to tell the keylogger what keys were pressed, where your mouse moves, and where your mouse clicks.
Secure Desktop [opens a new desktop](https://msdn.microsoft.com/en-us/library/windows/desktop/ms682124.aspx) and then opens the program you chose inside of it, Windows prevents programs from accessing desktops that they haven't been opened inside which means any keyloggers opened inside of your regular desktop cannot access your keyboard or mouse operations inside of the secure desktop and vice versa.

This technique is the same technique used in the UAC, ctrl+alt+del screen, and even the login screen to prevent keyloggers from logging your sensitive information and forcing you to click on things you don't want to (eg allowing administrative access to an application).

## Features

 * Runs under the .NET Framework 2.0
 * Strict cleanup system terminates orphan processes often created by malicious programs after the primary process has closed
 * Desktop agent that ensures cleanup and provides in-desktop hotkeys
   * Suppresses accidental PrintScreen presses in the event privacy is a concern (Hold ctrl to circumvent).
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

Secure Desktop cannot mitigate hardware keyloggers or Remote Administration Tools with a privilege level above [ring-3](https://en.wikipedia.org/wiki/Protection_ring).