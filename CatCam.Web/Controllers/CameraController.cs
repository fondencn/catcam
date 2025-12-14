using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CatCam.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CameraController : ControllerBase
{
    private readonly ILogger<CameraController> _logger;
    //private const string CameraDevice = "/dev/video0";

    public CameraController(ILogger<CameraController> logger)
    {
        _logger = logger;
    }

    [HttpGet("diagnostics")]
    public async Task<IActionResult> GetDiagnostics()
    {
        var diagnostics = new StringBuilder();
        
        diagnostics.AppendLine($"OS: {RuntimeInformation.OSDescription}");
        diagnostics.AppendLine($"Architecture: {RuntimeInformation.OSArchitecture}");
        diagnostics.AppendLine($"Is Linux: {RuntimeInformation.IsOSPlatform(OSPlatform.Linux)}");
        //diagnostics.AppendLine($"Camera device exists: {System.IO.File.Exists(CameraDevice)}");
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Check current user and groups
            try
            {
                var idInfo = new ProcessStartInfo
                {
                    FileName = "id",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(idInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    diagnostics.AppendLine($"\nCurrent user and groups: {output.Trim()}");
                }
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"\nError getting user info: {ex.Message}");
            }
            
            // Check device permissions
            diagnostics.AppendLine("\nDevice permissions:");
            var devicesToCheck = new[] { "/dev/video0", "/dev/v4l-subdev0", "/dev/v4l-subdev1", "/dev/media0", "/dev/dma_heap", "/dev/vchiq", "/dev/vcio" };
            foreach (var device in devicesToCheck)
            {
                try
                {
                    var lsInfo = new ProcessStartInfo
                    {
                        FileName = "ls",
                        Arguments = $"-la {device}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(lsInfo);
                    if (process != null)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        await process.WaitForExitAsync();
                        diagnostics.AppendLine($"  {output.Trim()}");
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.AppendLine($"  {device}: Error - {ex.Message}");
                }
            }
            
            // Check for available video devices
            diagnostics.AppendLine("\nChecking /dev/video* devices:");
            var devDir = new DirectoryInfo("/dev");
            if (devDir.Exists)
            {
                var videoDevices = devDir.GetFiles("video*");
                foreach (var device in videoDevices)
                {
                    diagnostics.AppendLine($"  - {device.FullName}");
                }
            }
            
            // Check for DMA heap and GPU devices (required for rpicam)
            diagnostics.AppendLine("\nChecking Raspberry Pi hardware access:");
            diagnostics.AppendLine($"  /dev/dma_heap: {(Directory.Exists("/dev/dma_heap") ? "Found" : "Not found")}");
            diagnostics.AppendLine($"  /dev/vchiq: {(System.IO.File.Exists("/dev/vchiq") ? "Found" : "Not found")}");
            diagnostics.AppendLine($"  /dev/vcio: {(System.IO.File.Exists("/dev/vcio") ? "Found" : "Not found")}");
            
            // Test rpicam-hello to list cameras
            try
            {
                var rpicamTest = new ProcessStartInfo
                {
                    FileName = "rpicam-hello",
                    Arguments = "--list-cameras",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(rpicamTest);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    diagnostics.AppendLine("\nrpicam-hello --list-cameras:");
                    if (!string.IsNullOrEmpty(output))
                    {
                        diagnostics.AppendLine(output);
                    }
                    if (!string.IsNullOrEmpty(error))
                    {
                        diagnostics.AppendLine("Errors:");
                        diagnostics.AppendLine(error);
                    }
                }
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"\nError running rpicam-hello: {ex.Message}");
            }

            // Check video device names in /sys/class/video4linux
            diagnostics.AppendLine("\nChecking video device names:");
            var v4lDir = new DirectoryInfo("/sys/class/video4linux");
            if (v4lDir.Exists)
            {
                foreach (var device in v4lDir.GetDirectories())
                {
                    var namePath = Path.Combine(device.FullName, "name");
                    if (System.IO.File.Exists(namePath))
                    {
                        var name = await System.IO.File.ReadAllTextAsync(namePath);
                        diagnostics.AppendLine($"  {device.Name}: {name.Trim()}");
                    }
                }
            }
            
            // Test rpicam-vid with --list-cameras
            try
            {
                var rpicamListInfo = new ProcessStartInfo
                {
                    FileName = "rpicam-vid",
                    Arguments = "--list-cameras",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(rpicamListInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    diagnostics.AppendLine("\nrpicam-vid --list-cameras:");
                    if (!string.IsNullOrEmpty(output))
                    {
                        diagnostics.AppendLine(output);
                    }
                    if (!string.IsNullOrEmpty(error))
                    {
                        diagnostics.AppendLine("Errors:");
                        diagnostics.AppendLine(error);
                    }
                    diagnostics.AppendLine($"Exit Code: {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"\nError running rpicam-vid --list-cameras: {ex.Message}");
            }

            // Test rpicam-vid version
            try
            {
                var rpicamVidInfo = new ProcessStartInfo
                {
                    FileName = "rpicam-vid",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(rpicamVidInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    diagnostics.AppendLine($"\nrpicam-vid version: {output.Split('\n')[0]}");
                }
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"\nrpicam-vid: Not found ({ex.Message})");
            }
            
            // Check for media devices (needed for libcamera)
            diagnostics.AppendLine("\nChecking /dev/media* devices:");
            var mediaDevices = devDir.GetFiles("media*");
            if (mediaDevices.Length > 0)
            {
                foreach (var device in mediaDevices)
                {
                    diagnostics.AppendLine($"  - {device.FullName}");
                }
            }
            else
            {
                diagnostics.AppendLine("  No media devices found");
            }
            
            // Check dmesg for camera-related messages
            try
            {
                var dmesgInfo = new ProcessStartInfo
                {
                    FileName = "dmesg",
                    Arguments = "| grep -i -E 'camera|unicam|imx|ov5647' | tail -20",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(dmesgInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        diagnostics.AppendLine("\nRecent camera-related kernel messages:");
                        diagnostics.AppendLine(output);
                    }
                }
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"\nError checking dmesg: {ex.Message}");
            }

            // // Test ffmpeg availability (kept for reference)
            // try
            // {
            //     var ffmpegInfo = new ProcessStartInfo
            //     {
            //         FileName = "ffmpeg",
            //         Arguments = "-version",
            //         RedirectStandardOutput = true,
            //         RedirectStandardError = true,
            //         UseShellExecute = false,
            //         CreateNoWindow = true
            //     };

            //     using var process = Process.Start(ffmpegInfo);
            //     if (process != null)
            //     {
            //         var output = await process.StandardOutput.ReadToEndAsync();
            //         await process.WaitForExitAsync();
            //         var firstLine = output.Split('\n')[0];
            //         diagnostics.AppendLine($"\nffmpeg: {firstLine}");
            //     }
            // }
            // catch (Exception ex)
            // {
            //     diagnostics.AppendLine($"\nError running ffmpeg: {ex.Message}");
            // }
        }
        
        return Content(diagnostics.ToString(), "text/plain");
    }

    [HttpGet("stream")]
    public async Task StreamVideo()
    {
        try
        {
            // Check if running on Linux (Raspberry Pi)
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _logger.LogWarning("Camera streaming is only supported on Linux/Raspberry Pi");
                Response.StatusCode = 501;
                await Response.WriteAsync("Camera streaming is only available on Raspberry Pi");
                return;
            }

            _logger.LogInformation("Starting camera stream with rpicam-vid");

            Response.ContentType = "multipart/x-mixed-replace; boundary=frame";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "close";

            // Use rpicam-vid for smooth video streaming
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "rpicam-vid",
                Arguments = "-t 0 --codec mjpeg --width 640 --height 480 --framerate 30 --inline -o -",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start rpicam-vid process");
                Response.StatusCode = 500;
                await Response.WriteAsync("Failed to start camera stream");
                return;
            }

            // Capture stderr for diagnostics
            _ = Task.Run(async () =>
            {
                var error = await process.StandardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogWarning("rpicam-vid stderr: {Error}", error);
                }
            });

            var stream = process.StandardOutput.BaseStream;
            var buffer = new List<byte>();
            var frameCount = 0;
            
            try
            {
                _logger.LogInformation("Parsing MJPEG stream from rpicam-vid");
                
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    var b = stream.ReadByte();
                    if (b == -1) break;
                    
                    buffer.Add((byte)b);
                    
                    // Detect JPEG start marker (0xFF 0xD8)
                    if (buffer.Count >= 2 && buffer[^2] == 0xFF && buffer[^1] == 0xD8 && buffer.Count > 2)
                    {
                        // Found start of new frame, send previous frame
                        var frameData = buffer.Take(buffer.Count - 2).ToArray();
                        buffer.Clear();
                        buffer.Add(0xFF);
                        buffer.Add(0xD8);
                        
                        if (frameData.Length > 100) // Valid frame
                        {
                            await Response.WriteAsync("--frame\r\n", HttpContext.RequestAborted);
                            await Response.WriteAsync("Content-Type: image/jpeg\r\n", HttpContext.RequestAborted);
                            await Response.WriteAsync($"Content-Length: {frameData.Length}\r\n\r\n", HttpContext.RequestAborted);
                            await Response.Body.WriteAsync(frameData, HttpContext.RequestAborted);
                            await Response.WriteAsync("\r\n", HttpContext.RequestAborted);
                            await Response.Body.FlushAsync(HttpContext.RequestAborted);
                            
                            frameCount++;
                            if (frameCount == 1)
                            {
                                _logger.LogInformation("First frame sent, size: {Size} bytes", frameData.Length);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Camera stream cancelled by client after {FrameCount} frames", frameCount);
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
                _logger.LogInformation("Camera stream ended, total frames sent: {FrameCount}", frameCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming camera video");
            if (!Response.HasStarted)
            {
                Response.StatusCode = 500;
                await Response.WriteAsync($"Error: {ex.Message}");
            }
        }
    }

    [HttpGet("snapshot")]
    public async Task<IActionResult> GetSnapshot()
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return StatusCode(501, "Camera snapshot is only supported on Linux/Raspberry Pi");
            }

            // if (!System.IO.File.Exists(CameraDevice))
            // {
            //     return NotFound($"Camera device {CameraDevice} not found");
            // }

            //_logger.LogInformation("Capturing snapshot from {Device}", CameraDevice);

            // Use rpicam-still for snapshot capture
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "rpicam-still",
                Arguments = "-t 1 --width 1280 --height 720 -o -",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                return StatusCode(500, "Failed to capture snapshot");
            }

            // Capture stderr for diagnostics
            var errorTask = process.StandardError.ReadToEndAsync();

            var imageData = new MemoryStream();
            await process.StandardOutput.BaseStream.CopyToAsync(imageData);
            await process.WaitForExitAsync();

            var errorOutput = await errorTask;
            if (!string.IsNullOrEmpty(errorOutput))
            {
                _logger.LogWarning("FFmpeg stderr during snapshot: {Error}", errorOutput);
            }

            if (process.ExitCode != 0)
            {
                _logger.LogError("Camera capture failed with exit code {ExitCode}", process.ExitCode);
                return StatusCode(500, $"Camera capture failed: {errorOutput}");
            }

            if (imageData.Length == 0)
            {
                _logger.LogError("Snapshot image is empty");
                return StatusCode(500, "Snapshot image is empty");
            }

            imageData.Position = 0;
            _logger.LogInformation("Snapshot captured successfully, size: {Size} bytes", imageData.Length);
            return File(imageData, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing snapshot");
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}
