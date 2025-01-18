namespace ClientApp.Tests
{
    [TestFixture]
    public class AuthControllerTests
    {
        private HttpClient? _client;
        private string? _token;

        [SetUp]
        public async Task Setup()
        {
            var factory = new ServerApplication();
            _client = factory.CreateClient();
            _token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        }

        [Test]
        public async Task Register_ValidUser_ReturnsSuccess()
        {
            var user = new { Username = $"testuser_{Guid.NewGuid()}", Password = "testpassword" };
            var content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");
            var response = await _client!.PostAsync("/api/auth/register", content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain("token");
        }

        [Test]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            var username = $"testuser_{Guid.NewGuid()}";
            var password = "testpassword";
            var registerContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var registerResponse = await _client!.PostAsync("/api/auth/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();
            var loginContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();
            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            loginResult.Should().Contain("token");
        }

        [Test]
        public async Task ChangePassword_ValidRequest_ReturnsNewToken()
        {
            var username = $"testuser_{Guid.NewGuid()}";
            var password = "testpassword";
            var registerContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var registerResponse = await _client!.PostAsync("/api/auth/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();
            var loginContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();
            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<JsonElement>(loginResult).GetProperty("token").GetString();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var newPassword = "newpassword";
            var changePasswordContent = new StringContent(JsonSerializer.Serialize(new { CurrentPassword = password, NewPassword = newPassword }), Encoding.UTF8, "application/json");
            var changePasswordResponse = await _client.PatchAsync("/api/password", changePasswordContent);
            changePasswordResponse.EnsureSuccessStatusCode();
            var changePasswordResult = await changePasswordResponse.Content.ReadAsStringAsync();
            changePasswordResult.Should().Contain("token");
        }

        [Test]
        public async Task AddContact_ValidRequest_ReturnsSuccess()
        {
            var contact = new { Name = "John Doe", PhoneNumber = "1234567890", Email = "john@example.com", Address = "123 Main St" };
            var content = new StringContent(JsonSerializer.Serialize(contact), Encoding.UTF8, "application/json");
            var response = await _client!.PostAsync("/api/contacts", content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain("Контакт успешно добавлен");
        }

        [Test]
        public async Task GetAllContacts_ReturnsContacts()
        {
            var response = await _client!.GetAsync("/api/contacts");
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task GetContact_ValidId_ReturnsContact()
        {
            var contact = new { Name = "Jane Doe", PhoneNumber = "0987654321", Email = "jane@example.com", Address = "456 Elm St" };
            var addContent = new StringContent(JsonSerializer.Serialize(contact), Encoding.UTF8, "application/json");
            var addResponse = await _client!.PostAsync("/api/contacts", addContent);
            addResponse.EnsureSuccessStatusCode();
            var addResponseString = await addResponse.Content.ReadAsStringAsync();
            var addResponseJson = JsonSerializer.Deserialize<JsonElement>(addResponseString);
            var contactId = addResponseJson.GetProperty("id").GetInt32();
            var response = await _client.GetAsync($"/api/contacts/{contactId}");
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseString);
            var contactJson = responseJson.GetProperty("contact");
            contactJson.GetProperty("name").GetString().Should().Be("Jane Doe");
            contactJson.GetProperty("phoneNumber").GetString().Should().Be("0987654321");
            contactJson.GetProperty("email").GetString().Should().Be("jane@example.com");
            contactJson.GetProperty("address").GetString().Should().Be("456 Elm St");
        }

        [Test]
        public async Task UpdateContact_ValidRequest_ReturnsSuccess()
        {
            var contact = new { Name = "Old Name", PhoneNumber = "1111111111", Email = "old@example.com", Address = "Old Address" };
            var addContent = new StringContent(JsonSerializer.Serialize(contact), Encoding.UTF8, "application/json");
            var addResponse = await _client!.PostAsync("/api/contacts", addContent);
            addResponse.EnsureSuccessStatusCode();
            var addResponseString = await addResponse.Content.ReadAsStringAsync();
            var addResponseJson = JsonSerializer.Deserialize<JsonElement>(addResponseString);
            var contactId = addResponseJson.GetProperty("id").GetInt32();
            var updatedContact = new { Name = "New Name", PhoneNumber = "2222222222", Email = "new@example.com", Address = "New Address" };
            var updateContent = new StringContent(JsonSerializer.Serialize(updatedContact), Encoding.UTF8, "application/json");
            var response = await _client.PatchAsync($"/api/contacts/{contactId}", updateContent);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseString);
            responseJson.GetProperty("message").GetString().Should().Be("Контакт успешно обновлен.");
        }

        [Test]
        public async Task DeleteContact_ValidId_ReturnsSuccess()
        {
            var contact = new { Name = "Delete Me", PhoneNumber = "3333333333", Email = "delete@example.com", Address = "Delete Address" };
            var addContent = new StringContent(JsonSerializer.Serialize(contact), Encoding.UTF8, "application/json");
            var addResponse = await _client!.PostAsync("/api/contacts", addContent);
            addResponse.EnsureSuccessStatusCode();
            var addResponseString = await addResponse.Content.ReadAsStringAsync();
            var addResponseJson = JsonSerializer.Deserialize<JsonElement>(addResponseString);
            var contactId = addResponseJson.GetProperty("id").GetInt32();
            var deleteResponse = await _client.DeleteAsync($"/api/contacts/{contactId}");
            deleteResponse.EnsureSuccessStatusCode();
            var deleteResponseString = await deleteResponse.Content.ReadAsStringAsync();
            var deleteResponseJson = JsonSerializer.Deserialize<JsonElement>(deleteResponseString);
            deleteResponseJson.GetProperty("message").GetString().Should().Be("Контакт успешно удален.");
        }

        [Test]
        public async Task SearchContacts_ValidRequest_ReturnsResults()
        {
            var searchRequest = new { Name = "John", PhoneNumber = "123", Email = "john", Address = "Main" };
            var content = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
            var response = await _client!.PostAsync("/api/contacts/search", content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task GetHistory_ReturnsHistory()
        {
            var response = await _client!.GetAsync("/api/history");
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task DeleteHistory_ReturnsSuccess()
        {
            var response = await _client!.DeleteAsync("/api/history");
            response.EnsureSuccessStatusCode();
        }

        private async Task<string> GetTokenAsync()
        {
            var username = $"testuser_{Guid.NewGuid()}";
            var password = "testpassword";
            var registerContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var registerResponse = await _client!.PostAsync("/api/auth/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();
            var loginContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();
            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<JsonElement>(loginResult).GetProperty("token").GetString();
            return token!;
        }
    }
}