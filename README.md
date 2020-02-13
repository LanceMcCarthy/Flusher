# The Flusher

Introducing The Flusher, a Windows IoT on Raspberry Pi, SignalR and XamarinForms project that uses AI to automatically flush a human toilet for my cat while I'm on vacation :)

## Parts and Services

* Windows IoT (17763) on Raspberry Pi 3 (B+)
    * 4 Tri-Color 5mm LEDs ([Amazon link](https://www.amazon.com/dp/B077X95LRZ/ref=cm_sw_em_r_mt_dp_U_7R2pEbEP5T7PP))
    * Micro Servo 9g FS90 ([Amazon link](https://www.amazon.com/dp/B07CM87WBQ/ref=cm_sw_em_r_mt_dp_U_DQ2pEbV1XCF1P))
    * Danco HydroStop Toilet flushing valve ([Danco link](https://www.danco.com/product/hydrostop-toilet-tank-flapper-alternative-toilet-repair-kit-flt231t/))
    * Push button (Normally open)
    * Logitech HD Webcam C310 ([Amazon link](https://www.amazon.com/dp/B003LVZO8S/ref=cm_sw_em_r_mt_dp_U_fU2pEbPJ9VMH4))
    * 3D printed case [See it in this Thingiverse project](https://www.thingiverse.com/make:760269)
- [Azure Custom Vision](customvision.ai)
  - Custom trained with at least 4 iterations and 8 hours of training time
  - Accessed via API
  - Offline inferencing capability via ONYX on Windows Machine Learning
- [Azure Storage API](https://docs.microsoft.com/en-us/rest/api/storageservices/blob-service-rest-api) (uses blob containers)
- [ASP.NET Core 3.1 (MVC and SignalR)](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-3.1)
- [Xamarin.Forms](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/) for Android, iOS and PC desktop admin applications

## Operation Details

The IoT client (a UWP app running on the Raspberry Pi) will take a photo of the toilet bowl and upload it to Azure blob. Once uploaded to the blob, the photo's URL is passed to Azure Custom Vision.  I have trained the AI to detect and locate the position of any substance in the bowl, it will return the results of the analysis to the IoT client. 

If the detection is 85% or higher probability that there is an undesirable substance, the servo angle will be changed from 0 degrees to 100 degrees for a duration of 5 seconds. This opens the toilet valve and flushes the toilet.

At all times, the IoT client is communicating information in real-time to all admin applications using SignalR.

## Admin Applications

The native admin apps have been compiled for Android, iOS and PC (UWP Desktop). There is also a web portal on the same SignalR server using MVC. The admin can manually take a photo or request an analysis at any time.
