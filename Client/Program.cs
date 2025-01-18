using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClientApp
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static string baseUrl = "http://localhost:5017/api";

        static async Task Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("Выберите действие:");
                Console.WriteLine("1. Регистрация");
                Console.WriteLine("2. Авторизация");
                Console.WriteLine("3. Получить историю запросов");
                Console.WriteLine("4. Удалить историю запросов");
                Console.WriteLine("5. Изменить пароль");
                Console.WriteLine("6. Добавить контакт");
                Console.WriteLine("7. Удалить контакт");
                Console.WriteLine("8. Редактировать контакт");
                Console.WriteLine("9. Просмотреть все контакты");
                Console.WriteLine("10. Просмотреть один контакт");
                Console.WriteLine("11. Поиск контактов");
                Console.WriteLine("12. Выйти");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await RegisterUser();
                        break;
                    case "2":
                        await LoginUser();
                        break;
                    case "3":
                        await GetRequestHistory();
                        break;
                    case "4":
                        await DeleteRequestHistory();
                        break;
                    case "5":
                        await ChangePassword();
                        break;
                    case "6":
                        await AddContact();
                        break;
                    case "7":
                        await DeleteContact();
                        break;
                    case "8":
                        await UpdateContact();
                        break;
                    case "9":
                        await GetAllContacts();
                        break;
                    case "10":
                        await GetContact();
                        break;
                    case "11":
                        await SearchContacts();
                        break;
                    case "12":
                        return;
                    default:
                        Console.WriteLine("Неверный выбор. Попробуйте снова.");
                        break;
                }
            }
        }

        private static async Task RegisterUser()
        {
            Console.Write("Введите имя пользователя: ");
            var username = Console.ReadLine();
            Console.Write("Введите пароль: ");
            var password = Console.ReadLine();

            try
            {
                var response = await httpClient.PostAsJsonAsync($"{baseUrl}/auth/register", new { Username = username, Password = password });

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();

                    if (responseJson.TryGetProperty("token", out var tokenProperty))
                    {
                        var token = tokenProperty.GetString();
                        Console.WriteLine($"Регистрация успешна. Токен: {token}");
                    }
                    else
                    {
                        Console.WriteLine("Ошибка: сервер не вернул токен.");
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. Ответ сервера: {errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при отправке запроса: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            }
        }

        private static async Task LoginUser()
        {
            Console.Write("Введите имя пользователя: ");
            var username = Console.ReadLine();
            Console.Write("Введите пароль: ");
            var password = Console.ReadLine();

            try
            {
                var response = await httpClient.PostAsJsonAsync($"{baseUrl}/auth/login", new { Username = username, Password = password });

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();

                    if (responseJson.TryGetProperty("token", out var tokenProperty))
                    {
                        var token = tokenProperty.GetString();
                        Console.WriteLine($"Авторизация успешна. Токен: {token}");
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                    else
                    {
                        Console.WriteLine("Ошибка: сервер не вернул токен.");
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. Ответ сервера: {errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при отправке запроса: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            }
        }

        private static async Task GetRequestHistory()
        {
            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/history");

                if (response.IsSuccessStatusCode)
                {
                    var history = await response.Content.ReadFromJsonAsync<JsonElement>();
                    Console.WriteLine("История запросов:");
                    Console.WriteLine(JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. Ответ сервера: {errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при отправке запроса: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            }
        }

        private static async Task DeleteRequestHistory()
        {
            try
            {
                var response = await httpClient.DeleteAsync($"{baseUrl}/history");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("История запросов удалена.");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. Ответ сервера: {errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при отправке запроса: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            }
        }

        private static async Task ChangePassword()
        {
            Console.Write("Введите текущий пароль: ");
            var currentPassword = Console.ReadLine();
            Console.Write("Введите новый пароль: ");
            var newPassword = Console.ReadLine();

            try
            {
                var response = await httpClient.PatchAsJsonAsync($"{baseUrl}/password", new { CurrentPassword = currentPassword, NewPassword = newPassword });

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();

                    if (responseJson.TryGetProperty("token", out var tokenProperty))
                    {
                        var token = tokenProperty.GetString();
                        Console.WriteLine($"Пароль изменен. Новый токен: {token}");
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                    else
                    {
                        Console.WriteLine("Ошибка: сервер не вернул токен.");
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. Ответ сервера: {errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при отправке запроса: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            }
        }

        private static async Task AddContact()
        {
            Console.Write("Введите имя контакта: ");
            var name = Console.ReadLine();
            Console.Write("Введите номер телефона: ");
            var phoneNumber = Console.ReadLine();
            Console.Write("Введите email (необязательно): ");
            var email = Console.ReadLine();
            Console.Write("Введите адрес (необязательно): ");
            var address = Console.ReadLine();

            try
            {
                var response = await httpClient.PostAsJsonAsync($"{baseUrl}/contacts", new { Name = name, PhoneNumber = phoneNumber, Email = email, Address = address });

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Контакт успешно добавлен.");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. Ответ сервера: {errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при отправке запроса: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            }
        }

        private static async Task DeleteContact()
        {
            Console.Write("Введите ID контакта для удаления: ");
            var id = Console.ReadLine();

            try
            {
                var response = await httpClient.DeleteAsync($"{baseUrl}/contacts/{id}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Контакт успешно удален.");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. Ответ сервера: {errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при отправке запроса: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            }
        }

        private static async Task UpdateContact()
        {
            Console.Write("Введите ID контакта для обновления: ");
            var id = Console.ReadLine();
            Console.Write("Введите новое имя контакта: ");
            var name = Console.ReadLine();
            Console.Write("Введите новый номер телефона: ");
            var phoneNumber = Console.ReadLine();
            Console.Write("Введите новый email (необязательно): ");
            var email = Console.ReadLine();
            Console.Write("Введите новый адрес (необязательно): ");
            var address = Console.ReadLine();

            try
            {
                var response = await httpClient.PatchAsJsonAsync($"{baseUrl}/contacts/{id}", new { Name = name, PhoneNumber = phoneNumber, Email = email, Address = address });

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Контакт успешно обновлен.");
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. Ответ сервера: {errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при отправке запроса: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            }
        }

        private static async Task GetAllContacts()
        {
            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/contacts");

                if (response.IsSuccessStatusCode)
                {
                    var contacts = await response.Content.ReadFromJsonAsync<JsonElement>();
                    Console.WriteLine("Список контактов:");
                    Console.WriteLine(JsonSerializer.Serialize(contacts, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. Ответ сервера: {errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при отправке запроса: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            }
        }

        private static async Task GetContact()
        {
            Console.Write("Введите ID контакта: ");
            var id = Console.ReadLine();

            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/contacts/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var contact = await response.Content.ReadFromJsonAsync<JsonElement>();
                    Console.WriteLine("Информация о контакте:");
                    Console.WriteLine(JsonSerializer.Serialize(contact, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. Ответ сервера: {errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при отправке запроса: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            }
        }

        private static async Task SearchContacts()
        {
            Console.Write("Введите имя для поиска (необязательно): ");
            var name = Console.ReadLine();
            Console.Write("Введите номер телефона для поиска (необязательно): ");
            var phoneNumber = Console.ReadLine();
            Console.Write("Введите email для поиска (необязательно): ");
            var email = Console.ReadLine();
            Console.Write("Введите адрес для поиска (необязательно): ");
            var address = Console.ReadLine();

            try
            {
                var response = await httpClient.PostAsJsonAsync($"{baseUrl}/contacts/search", new { Name = name, PhoneNumber = phoneNumber, Email = email, Address = address });

                if (response.IsSuccessStatusCode)
                {
                    var contacts = await response.Content.ReadFromJsonAsync<JsonElement>();
                    Console.WriteLine("Результаты поиска:");
                    Console.WriteLine(JsonSerializer.Serialize(contacts, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка: {response.StatusCode}. Ответ сервера: {errorResponse}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Ошибка при отправке запроса: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
            }
        }
    }
}