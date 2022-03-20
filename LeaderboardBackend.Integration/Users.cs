using NUnit.Framework;
using System.Net;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests.Users;
using LeaderboardBackend.Models.Entities;
using System.Net.Http.Json;
using System;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace LeaderboardBackend.Integration;

[TestFixture]
internal class Users
{
	private static TestApiFactory Factory = null!;
	private static HttpClient ApiClient = null!;
	private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	private static readonly string ValidPassword = "c00l_pAssword";

	[SetUp]
	public static void SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateClient();	
	}

	[Test]
	public static async Task GetUser_NotFound()
	{
		Guid randomGuid = new();
		HttpResponseMessage response = await ApiClient.GetAsync($"/api/users/{randomGuid}");
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Test]
	public static async Task GetUser_Found()
	{
		RegisterRequest registerBody = new() 
		{
			Username = "Test",
			Password = ValidPassword,
			PasswordConfirm = ValidPassword,
			Email = "test@email.com",
		};
		HttpResponseMessage registerResponse = await ApiClient.PostAsJsonAsync("/api/users/register", registerBody, JsonSerializerOptions);
		registerResponse.EnsureSuccessStatusCode();
		User createdUser = await HttpHelpers.ReadFromResponseBody<User>(registerResponse, JsonSerializerOptions);

		HttpResponseMessage userResponse = await ApiClient.GetAsync($"/api/users/{createdUser?.Id}");
		userResponse.EnsureSuccessStatusCode();
		User retrievedUser = await HttpHelpers.ReadFromResponseBody<User>(userResponse, JsonSerializerOptions);

		Assert.AreEqual(createdUser, retrievedUser);
	}

	[Test]
	public static async Task FullAuthFlow()
	{
		// Register User	
		RegisterRequest registerBody = new() 
		{
			Username = "Test",
			Password = ValidPassword,
			PasswordConfirm = ValidPassword,
			Email = "test@email.com",
		};
		HttpResponseMessage registerResponse = await ApiClient.PostAsJsonAsync("/api/users/register", registerBody, JsonSerializerOptions);
		registerResponse.EnsureSuccessStatusCode();
		User createdUser = await HttpHelpers.ReadFromResponseBody<User>(registerResponse, JsonSerializerOptions);

		// Login		
		LoginRequest loginBody = new()
		{
			Email = createdUser!.Email,
			Password = ValidPassword,
		};
		HttpResponseMessage loginResponse = await ApiClient.PostAsJsonAsync("/api/users/login", loginBody, JsonSerializerOptions);
		loginResponse.EnsureSuccessStatusCode();

		string token = (await HttpHelpers.ReadFromResponseBody<LoginResponse>(loginResponse, JsonSerializerOptions)).Token;

		// Me
		HttpRequestMessage meRequest = new(
			HttpMethod.Get,
			"/api/users/me")
		{
			Headers =
			{
				Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token)
			}
		};
		HttpResponseMessage meResponse = await ApiClient.SendAsync(meRequest);
		meResponse.EnsureSuccessStatusCode();	
		User meUser = await HttpHelpers.ReadFromResponseBody<User>(registerResponse, JsonSerializerOptions);
		Assert.AreEqual(createdUser, meUser);
	}

}
