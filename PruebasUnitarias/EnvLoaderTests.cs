using Dfit.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PruebasUnitarias;

[TestClass]
[TestCategory("Configuracion")]
[DoNotParallelize] // Variables de entorno: estado compartido del proceso.
public class EnvLoaderTests : PruebaDocumentada
{

    private string _tempEnvPath = string.Empty;
    private readonly Dictionary<string, string?> _valoresOriginales = new();

    [TestInitialize]
    public void Setup()
    {
        _tempEnvPath = Path.Combine(Path.GetTempPath(), $"dfit_env_test_{Guid.NewGuid():N}.tmp");
        _valoresOriginales.Clear();
    }

    private string NuevaVariable()
    {
        var clave = "DFIT_TEST_" + Guid.NewGuid().ToString("N");
        _valoresOriginales.Add(clave, Environment.GetEnvironmentVariable(clave));
        Environment.SetEnvironmentVariable(clave, null);
        return clave;
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            foreach (var variable in _valoresOriginales)
                Environment.SetEnvironmentVariable(variable.Key, variable.Value);
        }
        finally
        {
            if (File.Exists(_tempEnvPath))
                File.Delete(_tempEnvPath);
        }
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENV-001")]
    public void LoadFile_CargaVariablesSimplesYConComillas()
    {
        var simple = NuevaVariable();
        var quoted = NuevaVariable();
        File.WriteAllText(_tempEnvPath,
            $"# Comentario\n{simple}=valor_simple_123\n\n{quoted}=\"valor con espacios y comillas\"\n");

        EnvLoader.LoadFile(_tempEnvPath);

        Comprobar("valor_simple_123", Environment.GetEnvironmentVariable(simple));
        Comprobar("valor con espacios y comillas", Environment.GetEnvironmentVariable(quoted));
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENV-002")]
    public void LoadFile_NoSobrescribeVariableExistenteDelSistema()
    {
        var clave = NuevaVariable();
        Environment.SetEnvironmentVariable(clave, "valor_del_sistema");
        File.WriteAllText(_tempEnvPath, $"{clave}=valor_del_archivo_env");

        EnvLoader.LoadFile(_tempEnvPath);

        Comprobar("valor_del_sistema", Environment.GetEnvironmentVariable(clave));
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENV-003")]
    public void LoadFile_ValorMultilinea_ConservaSaltosDeLinea()
    {
        var clave = NuevaVariable();
        File.WriteAllText(_tempEnvPath, $"{clave}=\"linea1\nlinea2\"\n");

        EnvLoader.LoadFile(_tempEnvPath);

        Comprobar("linea1\nlinea2", Environment.GetEnvironmentVariable(clave));
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENV-004")]
    public void LoadFile_ArchivoInexistente_NoCreaArchivoNiAlteraVariable()
    {
        var clave = NuevaVariable();
        Environment.SetEnvironmentVariable(clave, "original");

        EnvLoader.LoadFile(_tempEnvPath);

        Comprobar("original", Environment.GetEnvironmentVariable(clave));
        Comprobar(false, File.Exists(_tempEnvPath));
    }

    [TestMethod]
    [TestProperty("Caso", "CP-ENV-005")]
    public void LoadFile_LineasInvalidas_IgnoraYConservaIgualesDentroDelValor()
    {
        var clave = NuevaVariable();
        File.WriteAllText(_tempEnvPath, $"# comentario\n\nlinea_sin_separador\n=sin_clave\n{clave}=uno=dos\n");

        EnvLoader.LoadFile(_tempEnvPath);

        Comprobar("uno=dos", Environment.GetEnvironmentVariable(clave));
    }
}
