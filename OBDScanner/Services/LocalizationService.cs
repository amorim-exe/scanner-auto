using System.ComponentModel;

namespace OBDScanner.Services;

public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly LocalizationService _instance = new();

    public static LocalizationService Instance => _instance;

    private string _currentLanguage = "en-US";

    private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["en-US"] = new()
        {
            ["app_title"] = "AutoScan",
            ["app_subtitle"] = "Vehicle Diagnostic System",
            ["status_disconnected"] = "DISCONNECTED",
            ["status_connected"] = "CONNECTED",
            ["modules"] = "MODULES",
            ["dashboard"] = "Dashboard",
            ["dashboard_description"] = "Vehicle overview",
            ["diagnostics"] = "Diagnostics",
            ["diagnostics_description"] = "Fault codes",
            ["terminal"] = "Terminal",
            ["terminal_description"] = "ELM327 commands",
            ["sensors"] = "Sensors",
            ["sensors_description"] = "Live data",
            ["settings"] = "Settings",
            ["settings_description"] = "Application settings",
            ["about"] = "About",
            ["about_description"] = "Application information",
            ["help"] = "HELP",
            ["close"] = "CLOSE",
            ["language"] = "Language",
            ["connection"] = "CONNECTION",
            ["refresh"] = "Refresh",
            ["connect"] = "Connect",
            ["disconnect"] = "Disconnect",
            ["send"] = "SEND",
            ["disconnected"] = "Disconnected",
            ["no_com_ports"] = "No COM ports found.",
            ["select_port"] = "Select a COM port.",
            ["connected_to"] = "Connected to",
            ["initializing"] = "Initializing ELM327...",
            ["terminal_serial"] = "SERIAL",
            ["rpm"] = "ENGINE RPM",
        },

        ["pt-BR"] = new()
        {
            ["app_title"] = "AutoScan",
            ["app_subtitle"] = "Sistema de Diagnóstico Veicular",
            ["status_disconnected"] = "DESCONECTADO",
            ["status_connected"] = "CONECTADO",
            ["modules"] = "MÓDULOS",
            ["dashboard"] = "Painel",
            ["dashboard_description"] = "Visão geral do veículo",
            ["diagnostics"] = "Diagnóstico",
            ["diagnostics_description"] = "Códigos de falha",
            ["terminal"] = "Terminal",
            ["terminal_description"] = "Comandos ELM327",
            ["sensors"] = "Sensores",
            ["sensors_description"] = "Dados em tempo real",
            ["settings"] = "Configurações",
            ["settings_description"] = "Configurações do aplicativo",
            ["about"] = "Sobre",
            ["about_description"] = "Informações do aplicativo",
            ["help"] = "AJUDA",
            ["close"] = "FECHAR",
            ["language"] = "Idioma",
            ["connection"] = "CONEXÃO",
            ["rpm"] = "RPM DO MOTOR",
            ["refresh"] = "Atualizar",
            ["connect"] = "Conectar",
            ["disconnect"] = "Desconectar",
            ["send"] = "ENVIAR",
            ["disconnected"] = "Desconectado",
            ["no_com_ports"] = "Nenhuma porta COM encontrada.",
            ["select_port"] = "Selecione uma porta COM.",
            ["connected_to"] = "Conectado em",
            ["initializing"] = "Inicializando ELM327...",
            ["terminal_serial"] = "SERIAL",
        },

        ["es-ES"] = new()
        {
            ["app_title"] = "AutoScan",
            ["app_subtitle"] = "Sistema de Diagnóstico del Vehículo",
            ["status_disconnected"] = "DESCONECTADO",
            ["status_connected"] = "CONECTADO",
            ["modules"] = "MÓDULOS",
            ["dashboard"] = "Panel",
            ["dashboard_description"] = "Vista general del vehículo",
            ["diagnostics"] = "Diagnóstico",
            ["diagnostics_description"] = "Códigos de falla",
            ["terminal"] = "Terminal",
            ["terminal_description"] = "Comandos ELM327",
            ["sensors"] = "Sensores",
            ["sensors_description"] = "Datos en tiempo real",
            ["settings"] = "Configuración",
            ["settings_description"] = "Configuración de la aplicación",
            ["rpm"] = "RPM DEL MOTOR",
            ["about"] = "Acerca de",
            ["about_description"] = "Información de la aplicación",
            ["help"] = "AYUDA",
            ["close"] = "CERRAR",
            ["language"] = "Idioma",
            ["connection"] = "CONEXIÓN",
            ["refresh"] = "Actualizar",
            ["connect"] = "Conectar",
            ["disconnect"] = "Desconectar",
            ["send"] = "ENVIAR",
            ["disconnected"] = "Desconectado",
            ["no_com_ports"] = "No se encontraron puertos COM.",
            ["select_port"] = "Seleccione un puerto COM.",
            ["connected_to"] = "Conectado a",
            ["initializing"] = "Inicializando ELM327...",
            ["terminal_serial"] = "SERIAL",
        }
    };

    public string CurrentLanguage => _currentLanguage;

    public string this[string key]
    {
        get
        {
            if (_translations.TryGetValue(_currentLanguage, out var language) &&
                language.TryGetValue(key, out var value))
            {
                return value;
            }

            return key;
        }
    }

    public string GetText(string key)
    {
        return this[key];
    }

    public void SetLanguage(string language)
    {
        if (!_translations.ContainsKey(language))
            return;

        _currentLanguage = language;

        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(string.Empty));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isDarkTheme = true;

    public bool IsDarkTheme => _isDarkTheme;

    public void ToggleTheme()
    {
        _isDarkTheme = !_isDarkTheme;

        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(nameof(IsDarkTheme)));
    }
}