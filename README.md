
# Tedd.VirtualNetworkCam
A virtual cam driver for Windows. Registers in the same way a web camera does (Video Capture Source Filter), can be used from applications such as Skype or Zoom. Sets up a TCP server and listens for connections. Upon connection information is sent to client, and client sends image to driver.
This can be used to send images via network into a "virtual webcam". An example of getting Kinect 2 depth information and body index, and using that to make a green screen effect is included.

## Installing
Compile `Tedd.VirtualNetworkCam` and locate the output folder. Copy these files to `C:\Tedd.VirtualNetworkCam`. Run `install.bat` and it will register the driver in this place (do not remove them from here without running `uninstall.bat`).

**Default listening port is 9090. This is hardcoded in VirtualCamFilter constructor.**

## Note on 32-bit/64-bit
You can change compile target in .Net to be 32-bit or 64-bit. It is important that you modify `install.bat` and `uninstall.bat` accordingly for it to work. The variable `set FRAMEWORKDIR=%windir%\Microsoft.NET\Framework\v4.0.30319` should be changed to `set FRAMEWORKDIR=%windir%\Microsoft.NET\Framework64\v4.0.30319` for 64-bit.

## Points of interest
### Debugging
Note that since it listens to TCP port 9090 then starting two apps that use camera will cause the last one to fail. There is a log file located at `%temp%\Tedd.VirtualNetworkCam.log`.

### Project: Tedd.VirtualNetworkCam
`VirtualCamFilter.cs` is the main entrypoint. This is also where request for new frames are being processed. It sets up a `NetworkCamServer.cs` for listen, which in turn creates `NetworkCamServerClient.cs` to handle each client connection. The code in these are a bit messy as I started with [System.IO.Pipelines](https://devblogs.microsoft.com/dotnet/system-io-pipelines-high-performance-io-in-net/) and had to rewrite as it got stuck on asyncronous read (something with COM and STA vs threadpool?).

### Tedd.VirtualNetworkCam.Client
Client library for sending images to driver. It connects, received information and has a method for sending data.

### Tedd.KinectNetworkCamClient
A WPF application that reads Kinect 2 image data, cuts out any people found and sends the image to the virtual camera over network using `Tedd.VirtualNetworkCam.Client`.

## Special thanks
This code was made possible by the DirectShow wrapper classes made by Maxim Kartavenkov aka Sonic back in 2012. Without them it would be way too much work. See his [Code Project](https://www.codeproject.com/Articles/Maxim-Kartavenkov) articles on the subject. Please note the copyright on his work.