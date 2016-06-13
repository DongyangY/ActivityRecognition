# ActivityRecognition
A Microsoft [Kinect v2](https://dev.windows.com/en-us/kinect) & [RFID](http://www.impinj.com) based real-time multi-people pre-defined activity recognition WPF desktop application.

## Features
* Position tracking
* Template (e.g., furniture) tracking
* Body orientation estimation
* Discrete posture recognition
* Object use detection

## Installation
Microsoft Visual Studio

## Usage
> What if I have only a Kinect v2 but no RFID reader and tags?

Disable the object use detection in code and do not select objects when defining activities.
```C#
// MainWindow.xaml.cs
private bool isRFIDRequired = false;
```


