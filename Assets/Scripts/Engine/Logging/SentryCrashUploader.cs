using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Logging;
using Game.Core.Logging.CrashReports;
using Game.Core.Logging.CrashUpload;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Engine.Logging
{
    /// <summary>
    /// 把玩家同意发送的崩溃包发到 Sentry。一次发一个，发成功了记下"已发送"；没发成（没网、Sentry 拒收）就留着，下次启动再发。
    /// 两个时机：启动时把以前没发出去的补发；玩家在崩溃界面上点"发送"时马上发。要在主线程上用。
    /// </summary>
    internal sealed class SentryCrashUploader : IDisposable
    {
        private static readonly LogCategory LogCrash = new LogCategory("Crash");

        private readonly SentryDsn _dsn;
        private readonly SentryCrashEnvelope _envelope;
        private readonly CrashUploadQueue _queue;
        private readonly string _clientName;
        private readonly Queue<string> _waiting = new Queue<string>();
        private bool _sending;
        private bool _disposed;

        public SentryCrashUploader(SentryDsn dsn, CrashUploadQueue queue, string release, string environment)
        {
            _dsn = dsn ?? throw new ArgumentNullException(nameof(dsn));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _envelope = new SentryCrashEnvelope(dsn, release, environment);
            _clientName = "game-crash-report/" + release;
            _queue.ReportApproved += Enqueue;
        }

        /// <summary>把以前同意了、还没发出去的崩溃包排上队。</summary>
        public void UploadPending()
        {
            foreach (string directory in _queue.PendingUploads())
            {
                Enqueue(directory);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _queue.ReportApproved -= Enqueue;
        }

        private void Enqueue(string reportDirectory)
        {
            if (!_waiting.Contains(reportDirectory))
            {
                _waiting.Enqueue(reportDirectory);
            }

            SendNext();
        }

        private void SendNext()
        {
            if (_sending || _disposed || _waiting.Count == 0)
            {
                return;
            }

            string directory = _waiting.Dequeue();
            byte[] body;

            // 最外层：包打不成（目录被删了、文件被占着）就记一条错误，跳过这个包，接着发下一个。
            try
            {
                body = _envelope.Build(directory, Guid.NewGuid(), DateTime.UtcNow);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                Log.Exception(LogCrash, exception, "崩溃包打不成要发的样子，跳过 " + directory);
                SendNext();
                return;
            }

            var request = new UnityWebRequest(_dsn.EnvelopeUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(body),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            request.SetRequestHeader("Content-Type", "application/x-sentry-envelope");
            request.SetRequestHeader("X-Sentry-Auth", _dsn.AuthHeader(_clientName));

            _sending = true;
            request.SendWebRequest().completed += _ => OnSent(request, directory);
        }

        private void OnSent(UnityWebRequest request, string directory)
        {
            _sending = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                _queue.MarkUploaded(directory);
                Log.Info(LogCrash, "崩溃包已发到 Sentry：" + directory);
            }
            else
            {
                Log.Warning(LogCrash, "崩溃包没发出去，下次启动再发（" + request.responseCode + " " + request.error + "）：" + directory);
            }

            request.Dispose();
            SendNext();
        }
    }
}
