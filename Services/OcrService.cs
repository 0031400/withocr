using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace withocr.Services;

public sealed class OcrService
{
    private const string Language = "chi_sim+eng";

    public async Task<string> RecognizeAsync(
        string tesseractPath,
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tesseractPath))
        {
            throw new InvalidOperationException("尚未设置 Tesseract 程序路径。");
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("图片文件不存在。", imagePath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = tesseractPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(imagePath);
        startInfo.ArgumentList.Add("stdout");
        startInfo.ArgumentList.Add("-l");
        startInfo.ArgumentList.Add(Language);

        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("无法启动 Tesseract。");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"无法启动 Tesseract，请检查程序路径：{tesseractPath}",
                ex);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? $"Tesseract 执行失败，退出码：{process.ExitCode}"
                    : error.Trim());
        }

        return output.Trim();
    }
}