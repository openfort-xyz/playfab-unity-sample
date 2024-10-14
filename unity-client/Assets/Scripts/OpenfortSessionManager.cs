using System;
using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.CloudScriptModels;
using UnityEngine;

public class OpenfortSessionManager
{
    public static async UniTask<string> GetEncryptionSession()
    {
        var request = new ExecuteFunctionRequest
        {
            FunctionName = "RegisterRecoverySession",
            FunctionParameter = new { },
            GeneratePlayStreamEvent = true
        };

        var taskCompletionSource = new UniTaskCompletionSource<string>();

        PlayFabCloudScriptAPI.ExecuteFunction(request, result =>
        {
            if (result.Error != null)
            {
                Debug.LogError($"Failed to create encryption session: {result.Error.Message}");
                taskCompletionSource.TrySetException(new Exception("Failed to create encryption session"));
            }
            else
            {
                var jsonResponse = result.FunctionResult.ToString();
                SessionResponse response = JsonUtility.FromJson<SessionResponse>(jsonResponse);
                taskCompletionSource.TrySetResult(response.session);
            }
        }, error =>
        {
            Debug.LogError($"Failed to create encryption session: {error.ErrorMessage}");
            taskCompletionSource.TrySetException(new Exception("Failed to create encryption session"));
        });

        return await taskCompletionSource.Task;
    }
}

[Serializable]
public class SessionResponse
{
    public string session;
}