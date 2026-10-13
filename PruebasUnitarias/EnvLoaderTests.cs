using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dfit.Utils;

namespace PruebasUnitarias;

[TestClass]
public class EnvLoaderTests
{
    private string _tempEnvPath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempEnvPath = Path.Combine(Path.GetTempPath(), $".env_test_{Guid.NewGuid():N}");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempEnvPath))
        {
            File.Delete(_tempEnvPath);
        }
    }

    [TestMethod]
    public void LoadFile_CargaVariablesSimplesYConComillas()
    {
        var testKey1 = "TEST_ENV_VAR_SIMPLE_" + Guid.NewGuid().ToString("N");
        var testKey2 = "TEST_ENV_VAR_QUOTED_" + Guid.NewGuid().ToString("N");

        var content = $@"
# Comentario de prueba
{testKey1}=valor_simple_123

{testKey2}=""valor con espacios y comillas""
";
        File.WriteAllText(_tempEnvPath, content);

        EnvLoader.LoadFile(_tempEnvPath);

        Assert.AreEqual("valor_simple_123", Environment.GetEnvironmentVariable(testKey1));
        Assert.AreEqual("valor con espacios y comillas", Environment.GetEnvironmentVariable(testKey2));

        // Limpieza de variables
        Environment.SetEnvironmentVariable(testKey1, null);
        Environment.SetEnvironmentVariable(testKey2, null);
    }

    [TestMethod]
    public void LoadFile_NoSobrescribeVariableExistenteDelSistema()
    {
        var testKey = "TEST_PREEXISTING_VAR_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(testKey, "valor_del_sistema");

        var content = $"{testKey}=valor_del_archivo_env";
        File.WriteAllText(_tempEnvPath, content);

        EnvLoader.LoadFile(_tempEnvPath);

        // Debe mantener la del sistema (prioridad Render / SO)
        Assert.AreEqual("valor_del_sistema", Environment.GetEnvironmentVariable(testKey));

        // Limpieza
        Environment.SetEnvironmentVariable(testKey, null);
    }
}
