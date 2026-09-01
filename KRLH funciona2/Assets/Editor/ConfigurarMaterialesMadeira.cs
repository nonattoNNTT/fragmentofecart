using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Script de Editor para configurar automaticamente os materiais
/// dos modelos de madeira importados do Blender.
/// 
/// Como usar:
///   Menu → Tools → Configurar Materiais dos Modelos de Madeira
/// </summary>
public class ConfigurarMaterialesMadeira : EditorWindow
{
    // Caminho do FBX dentro do projeto
    private const string CAMINHO_FBX = "Assets/Art/Modelos quarto/modelos_madeira.fbx";

    // Pasta onde estão as texturas
    private const string PASTA_TEXTURAS = "Assets/Art/Modelos quarto/textures";

    // Pasta onde os materiais serão salvos/extraídos
    private const string PASTA_MATERIAIS = "Assets/Art/Modelos quarto/Materials";

    [MenuItem("Tools/Configurar Materiais dos Modelos de Madeira")]
    public static void Configurar()
    {
        Debug.Log("=== Iniciando configuração dos materiais ===");

        // 1. Garante que a pasta de materiais existe
        if (!AssetDatabase.IsValidFolder(PASTA_MATERIAIS))
        {
            AssetDatabase.CreateFolder("Assets/Art/Modelos quarto", "Materials");
            Debug.Log($"Pasta criada: {PASTA_MATERIAIS}");
        }

        // 2. Pega o importador do FBX
        ModelImporter importer = AssetImporter.GetAtPath(CAMINHO_FBX) as ModelImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog(
                "Erro",
                $"FBX não encontrado em:\n{CAMINHO_FBX}\n\nVerifique se o arquivo está no projeto.",
                "OK"
            );
            Debug.LogError($"FBX não encontrado: {CAMINHO_FBX}");
            return;
        }

        // 3. Configura o importador para extrair os materiais
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation    = ModelImporterMaterialLocation.External;
        importer.materialName        = ModelImporterMaterialName.BasedOnMaterialName;
        importer.materialSearch      = ModelImporterMaterialSearch.Everywhere;

        // 4. Extrai os materiais do FBX para a pasta Materials
        importer.ExtractTextures(PASTA_TEXTURAS);
        AssetDatabase.WriteImportSettingsIfDirty(CAMINHO_FBX);
        AssetDatabase.ImportAsset(CAMINHO_FBX, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        // 5. Remap das texturas nos materiais extraídos
        AtribuirTexturasAosMateriais();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "✅ Concluído!",
            "Materiais configurados com sucesso!\n\n" +
            "Verifique os materiais em:\n" +
            PASTA_MATERIAIS +
            "\n\nSe ainda aparecer branco, arraste o modelo para a cena e confira os materiais no Inspector.",
            "OK"
        );

        Debug.Log("=== Configuração concluída! ===");
    }

    private static void AtribuirTexturasAosMateriais()
    {
        // Carrega todas as texturas da pasta
        string[] guidsTexturas = AssetDatabase.FindAssets("t:Texture2D", new[] { PASTA_TEXTURAS });
        var mapaTexturas = new Dictionary<string, Texture2D>();

        foreach (string guid in guidsTexturas)
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
            if (tex != null)
            {
                // Indexa por nome (sem extensão, em maiúsculo para comparação)
                string nome = Path.GetFileNameWithoutExtension(caminho).ToUpper();
                mapaTexturas[nome] = tex;
                Debug.Log($"Textura carregada: {nome} → {caminho}");
            }
        }

        // Também pega texturas da pasta textures raiz (que já existiam no projeto)
        string[] guidsTxtRaiz = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Modelos quarto" });
        foreach (string guid in guidsTxtRaiz)
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
            if (tex != null)
            {
                string nome = Path.GetFileNameWithoutExtension(caminho).ToUpper();
                if (!mapaTexturas.ContainsKey(nome))
                    mapaTexturas[nome] = tex;
            }
        }

        // Carrega todos os materiais extraídos
        string[] guidsMateriais = AssetDatabase.FindAssets("t:Material", new[] { PASTA_MATERIAIS });

        if (guidsMateriais.Length == 0)
        {
            // Se não extraiu para a pasta Materials, tenta na pasta textures
            guidsMateriais = AssetDatabase.FindAssets("t:Material", new[] { PASTA_TEXTURAS });
        }

        Debug.Log($"Materiais encontrados: {guidsMateriais.Length}");

        foreach (string guid in guidsMateriais)
        {
            string caminhoMat = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(caminhoMat);
            if (mat == null) continue;

            string nomeMat = mat.name.ToUpper();
            Debug.Log($"Processando material: {mat.name}");

            // Tenta encontrar uma textura com nome parecido
            Texture2D texEncontrada = null;

            // Busca exata
            if (mapaTexturas.TryGetValue(nomeMat, out texEncontrada))
            {
                Debug.Log($"  → Textura exata encontrada: {texEncontrada.name}");
            }
            else
            {
                // Busca parcial — útil p/ nomes como "Material.002" → "UNTITLED.002"
                foreach (var kv in mapaTexturas)
                {
                    if (kv.Key.Contains(nomeMat) || nomeMat.Contains(kv.Key))
                    {
                        texEncontrada = kv.Value;
                        Debug.Log($"  → Textura parcial encontrada: {texEncontrada.name}");
                        break;
                    }
                }
            }

            if (texEncontrada != null)
            {
                // Usa shader Universal Render Pipeline se disponível, senão Standard
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", texEncontrada);  // URP
                }
                else if (mat.HasProperty("_MainTex"))
                {
                    mat.SetTexture("_MainTex", texEncontrada);  // Standard
                }

                EditorUtility.SetDirty(mat);
                Debug.Log($"  ✓ Textura aplicada em: {mat.name}");
            }
            else
            {
                Debug.LogWarning($"  ⚠ Nenhuma textura encontrada para: {mat.name}");
            }
        }
    }
}
