# Flusher
A Windows IoT on Raspberry Pi, SignalR and XamarinForms project.

[Thingiverse Project](https://www.thingiverse.com/make:760269)

Introducing `The Flusher`, this complete solution combines:

* Windows IoT (17763) on Raspberry Pi 3 (B+)
    * 4 RGB LEDs
    * Servo connected to a toilet flushing valve
    * Push button
    * 1080p webcam
- A custom trained AI (Azure Custom Vision AI)
    - Accessed via API
    - Offline inferencing capability via ONYX on Windows ML
- Azure Storage (uses blob storage)
- ASP.NET Core 3.1 (MVC and SignalR)
- Xamarin.Forms for Android, iOS and PC desktop admin applications

## Operation

The IoT client (a UWP app running on the Raspberry Pi) will take a photo of the toilet bowl and upload it to Azure blob. Once uploaded to the blob, the photo's URL is passed to Azure Custom Vision.  I have trained the AI to detect and locate the position of any substance in the bowl, it will return the results of the analysis to the IoT client. 

If the detection is 85% or higher probability that there is an undesirable substance, the servo angle will be changed from 0 degrees to 100 degrees for a duration of 5 seconds. This opens the toilet valve and flushes the toilet.

At all times, the IoT client is communicating information in real-time to all admin applications using SignalR.

## Admin Apps

The native admin apps have been compiled for Android, iOS and PC. There is also a web portal on the same SignalR server using MVC. The admin can manually take a photo or request an analysis at any time.
