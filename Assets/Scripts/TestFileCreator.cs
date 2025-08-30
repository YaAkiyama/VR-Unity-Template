using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// Meta Quest用テストファイル作成ツール
/// 各フォルダにテストファイルを作成して読み取り確認
/// </summary>
public class TestFileCreator : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(CreateTestFilesCoroutine());
    }
    
    System.Collections.IEnumerator CreateTestFilesCoroutine()
    {
        // 権限取得を待つ
        yield return new WaitForSeconds(3f);
        
        Debug.Log("[TestFileCreator] === テストファイル作成開始 ===");
        
        // テストファイルを作成するパス
        string[] testPaths = {
            "/sdcard/Download",
            "/sdcard/Documents", 
            "/sdcard/Pictures",
            "/storage/emulated/0/Download",
            "/storage/emulated/0/Documents",
            Application.persistentDataPath
        };
        
        foreach (string path in testPaths)
        {
            CreateTestFile(path);
        }
        
        Debug.Log("[TestFileCreator] === テストファイル作成完了 ===");
        
        // 作成後、ファイルを確認
        yield return new WaitForSeconds(1f);
        VerifyFiles();
    }
    
    void CreateTestFile(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"[TestFileCreator] フォルダが存在しません: {folderPath}");
            return;
        }
        
        string testFileName = $"test_file_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string fullPath = Path.Combine(folderPath, testFileName);
        
        try
        {
            // テストファイルを作成
            string content = $"Test file created by VR Unity Template\n";
            content += $"Created at: {System.DateTime.Now}\n";
            content += $"Location: {folderPath}\n";
            content += $"Device: Meta Quest\n";
            
            File.WriteAllText(fullPath, content, Encoding.UTF8);
            
            Debug.Log($"[TestFileCreator] ✓ ファイル作成成功: {fullPath}");
            
            // 作成したファイルが読み取れるか確認
            if (File.Exists(fullPath))
            {
                Debug.Log($"[TestFileCreator] ✓ ファイル存在確認: {testFileName}");
                
                // ファイル内容を読み取り
                string readContent = File.ReadAllText(fullPath);
                Debug.Log($"[TestFileCreator] ✓ ファイル読み取り成功: {readContent.Length}文字");
            }
            else
            {
                Debug.LogWarning($"[TestFileCreator] ✗ 作成したファイルが見つかりません: {fullPath}");
            }
        }
        catch (System.UnauthorizedAccessException e)
        {
            Debug.LogError($"[TestFileCreator] ✗ アクセス拒否: {folderPath} - {e.Message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TestFileCreator] ✗ エラー: {folderPath} - {e.Message}");
        }
    }
    
    void VerifyFiles()
    {
        Debug.Log("[TestFileCreator] === ファイル検証開始 ===");
        
        string[] checkPaths = {
            "/sdcard/Download",
            "/sdcard/Documents",
            "/storage/emulated/0/Download",
            "/storage/emulated/0/Documents",
            Application.persistentDataPath
        };
        
        foreach (string path in checkPaths)
        {
            if (!Directory.Exists(path))
            {
                Debug.Log($"[TestFileCreator] フォルダなし: {path}");
                continue;
            }
            
            try
            {
                // Directory.GetFiles
                string[] files = Directory.GetFiles(path);
                Debug.Log($"[TestFileCreator] {path}:");
                Debug.Log($"  Directory.GetFiles: {files.Length}個");
                
                if (files.Length > 0)
                {
                    for (int i = 0; i < Mathf.Min(3, files.Length); i++)
                    {
                        string fileName = Path.GetFileName(files[i]);
                        FileInfo fi = new FileInfo(files[i]);
                        Debug.Log($"    - {fileName} ({fi.Length}bytes)");
                    }
                }
                
                // 特定のパターンで検索
                string[] txtFiles = Directory.GetFiles(path, "*.txt");
                if (txtFiles.Length > 0)
                {
                    Debug.Log($"  *.txtファイル: {txtFiles.Length}個");
                }
                
                // test_で始まるファイルを検索
                string[] testFiles = Directory.GetFiles(path, "test_*");
                if (testFiles.Length > 0)
                {
                    Debug.Log($"  test_*ファイル: {testFiles.Length}個");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TestFileCreator] 検証エラー: {path} - {e.Message}");
            }
        }
        
        Debug.Log("[TestFileCreator] === ファイル検証完了 ===");
    }
}