namespace CuisineerTweaks;

public static class Lang
{
    internal static string GetReloadedConfigMessage()
    {
        return LanguageSettings.Language switch
        {
            LANGUAGE.EN => "Cuisineer Tweaks Configuration Reloaded!",
            LANGUAGE.ZHTW => "Cuisineer Tweaks 配置已重新載入！",
            LANGUAGE.ZHCN => "Cuisineer Tweaks 配置已重新加载！",
            LANGUAGE.JA => "Cuisineer Tweaks の設定が再読み込みされました！",
            LANGUAGE.KO => "Cuisineer Tweaks 구성이 다시 로드되었습니다!",
            LANGUAGE.DE => "Cuisineer Tweaks Konfiguration wurde neu geladen!",
            LANGUAGE.FR => "Cuisineer Tweaks Configuration rechargée!",
            LANGUAGE.ES => "¡Cuisineer Tweaks Configuración ha sido recargada!",
            LANGUAGE.BRPT => "Cuisineer Tweaks Configuração recarregada!",
            _ => "Cuisineer Tweaks Configuration Reloaded!"
        };

    }
}