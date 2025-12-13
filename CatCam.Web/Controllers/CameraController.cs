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
    private const string CameraDevice = "/dev/video0";

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
        diagnostics.AppendLine($"Camera device exists: {System.IO.File.Exists(CameraDevice)}");
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
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
            
            // Test v4l2-ctl to get camera info
            try
            {
                var v4lInfo = new ProcessStartInfo
                {
                    FileName = "v4l2-ctl",
                    Arguments = $"--device={CameraDevice} --list-formats-ext",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(v4lInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    diagnostics.AppendLine("\nv4l2-ctl output:");
                    diagnostics.AppendLine(output);
                    if (!string.IsNullOrEmpty(error))
                    {
                        diagnostics.AppendLine("\nv4l2-ctl errors:");
                        diagnostics.AppendLine(error);
                    }
                }
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"\nError running v4l2-ctl: {ex.Message}");
            }

            // Test ffmpeg availability
            try
            {
                var ffmpegInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(ffmpegInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    var firstLine = output.Split('\n')[0];
                    diagnostics.AppendLine($"\nffmpeg: {firstLine}");
                }
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"\nError running ffmpeg: {ex.Message}");
            }
        }
        
        return Content(diagnostics.ToString(), "text/plain");
    }

    [HttpGet("stream")]
    public async Task StreamVideo()
    {
        Response.ContentType = "multipart/x-mixed-replace; boundary=frame";
        Response.Headers.CacheControl = "no-cache";

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

            // Check if camera device exists
            if (!System.IO.File.Exists(CameraDevice))
            {
                _logger.LogError("Camera device {Device} not found", CameraDevice);
                Response.StatusCode = 404;
                await Response.WriteAsync($"Camera device {CameraDevice} not found");
                return;
            }

            _logger.LogInformation("Starting camera stream from {Device}", CameraDevice);

            // Use rpicam-vid for Raspberry Pi camera streaming
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "rpicam-vid",
                Arguments = "-t 0 --codec mjpeg --width 640 --height 480 --framerate 15 -o -",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start camera process");
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
                    _logger.LogWarning("FFmpeg stderr: {Error}", error);
                }
            });

            var stream = process.StandardOutput.BaseStream;
            var buffer = new byte[4096];
            var totalBytes = 0;
            
            try
            {
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    var bytesRead = await stream.ReadAsync(buffer, HttpContext.RequestAborted);
                    if (bytesRead == 0)
                    {
                        _logger.LogWarning("Stream ended, total bytes: {TotalBytes}", totalBytes);
                        break;
                    }

                    totalBytes += bytesRead;
                    await Response.Body.WriteAsync(buffer.AsMemory(0, bytesRead), HttpContext.RequestAborted);
                    await Response.Body.FlushAsync(HttpContext.RequestAborted);
                    
                    if (totalBytes == bytesRead)
                    {
                        _logger.LogInformation("First {Bytes} bytes sent", bytesRead);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Camera stream cancelled by client after {TotalBytes} bytes", totalBytes);
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
                _logger.LogInformation("Camera stream ended, total bytes sent: {TotalBytes}", totalBytes);
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

            if (!System.IO.File.Exists(CameraDevice))
            {
                return NotFound($"Camera device {CameraDevice} not found");
            }

            _logger.LogInformation("Capturing snapshot from {Device}", CameraDevice);

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
