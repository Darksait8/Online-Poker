using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
using UnityEngine;

/// <summary>
/// Вспомогательный класс для определения локального IP-адреса
/// Полезно для настройки сервера
/// </summary>
public static class ServerDiscoveryHelper
{
    /// <summary>
    /// Получает локальный IP-адрес этого устройства
    /// </summary>
    public static string GetLocalIPAddress()
    {
        try
        {
            // Пробуем получить IP через Dns
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    // Пропускаем localhost
                    if (!ip.ToString().StartsWith("127."))
                    {
                        return ip.ToString();
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Не удалось получить IP через Dns: {ex.Message}");
        }
        
        // Альтернативный способ через NetworkInterface
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            
            foreach (var ni in networkInterfaces)
            {
                var ipProps = ni.GetIPProperties();
                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !addr.Address.ToString().StartsWith("127."))
                    {
                        return addr.Address.ToString();
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Не удалось получить IP через NetworkInterface: {ex.Message}");
        }
        
        Debug.LogWarning("Не удалось определить локальный IP-адрес. Используйте 'localhost' для локального подключения.");
        return "localhost";
    }
    
    /// <summary>
    /// Проверяет доступность сервера по указанному адресу и порту
    /// </summary>
    public static bool TestServerConnection(string host, int port, int timeoutMs = 2000)
    {
        try
        {
            using (var client = new TcpClient())
            {
                var result = client.BeginConnect(host, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(System.TimeSpan.FromMilliseconds(timeoutMs));
                
                if (success)
                {
                    client.EndConnect(result);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Выводит информацию о сетевых интерфейсах в консоль Unity
    /// Полезно для отладки
    /// </summary>
    [UnityEngine.ContextMenu("Show Network Info")]
    public static void ShowNetworkInfo()
    {
        Debug.Log("=== Информация о сети ===");
        Debug.Log($"Локальный IP: {GetLocalIPAddress()}");
        
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            Debug.Log($"Имя хоста: {host.HostName}");
            Debug.Log("Все IP-адреса:");
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Debug.Log($"  - {ip}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка получения информации о сети: {ex.Message}");
        }
    }
}

