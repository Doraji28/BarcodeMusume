using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Android;

public class Barcode : MonoBehaviour
{
    private CancellationTokenSource cancelToken;

    public async void ScanStart(CancellationToken token)
    {
        try
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                while (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                {
                    await UniTask.Yield(cancellationToken: token);
                }
            }
#endif
            await UniTask.Yield(cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("스캔이 취소되었습니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"조떔 {ex.Message}");
        }
    }

    private void TokenClear()
    {
        if (cancelToken != null)
        {
            cancelToken.Cancel();
            cancelToken.Dispose();
            cancelToken = null;
        }
    }
    private void OffCamera()
    {
    }
    private void OnDestroy()
    {
        TokenClear();
    }
}
