using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// タイプの取得を行う
/// </summary>
public static class TypeGetter
{
    static private Dictionary<string, List<Type>> typeDict;
    static MonoScript[] monoScripts;

    /// <summary>
    /// プロジェクト内に存在する全スクリプトファイル
    /// </summary>
    static MonoScript[] MonoScripts { get { return monoScripts ?? (monoScripts = Resources.FindObjectsOfTypeAll<MonoScript>().ToArray()); } }

    /// <summary>
    /// クラス名からタイプを取得する
    /// </summary>
    public static Type GetType(string className)
    {
        if (typeDict == null)
        {
            // Dictionary作成
            typeDict = new Dictionary<string, List<Type>>();
            foreach (var type in GetAllTypes())
            {
                if (!typeDict.ContainsKey(type.Name))
                {
                    typeDict.Add(type.Name, new List<Type>());
                }
                typeDict[type.Name].Add(type);
            }
        }

        if (typeDict.ContainsKey(className)) // クラスが存在
        {
            return typeDict[className][0];
        }
        else
        {
            // クラスが存在しない場合
            return null;
        }
    }

    /// <summary>
    /// 全てのクラスタイプを取得
    /// </summary>
    private static IEnumerable<Type> GetAllTypes()
    {
        // Unity標準のクラスタイプ
        var buitinTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .Where(type => type != null && !string.IsNullOrEmpty(type.Namespace))
            .Where(type => type.Namespace.Contains("UnityEngine"));

        // 自作のクラスタイプ
        var myTypes = MonoScripts
            .Where(script => script != null)
            .Select(script => script.GetClass())
            .Where(classType => classType != null)
            .Where(classType => classType.Module.Name == "Assembly-CSharp.dll");

        return buitinTypes.Concat(myTypes)
            .Distinct();
    }
}
