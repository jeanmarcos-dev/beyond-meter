using System.Net;
using System.Net.Http.Headers;
using AionMeter.Core.Assets;

namespace AionMeter.Tests;

public sealed class AssetCacheTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("asset-cache").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Data => Path.Combine(_dir, "data");

    private string Cache => Path.Combine(_dir, "cache");

    [Fact]
    public async Task A_skill_icon_downloads_once_and_then_comes_from_disk()
    {
        var server = new FakeServer(HttpStatusCode.OK, "image/png");
        using var icons = new SkillIconCache(Data, Cache, enabled: true, server);
        var ready = new TaskCompletionSource<string>();
        icons.IconReady += name => ready.TrySetResult(name);

        Assert.Null(icons.FileOf(11_020_000));
        Assert.Equal("ICON_GL_SKILL_002", await ready.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        var file = icons.FileOf(11_020_000);

        Assert.Equal(Path.Combine(Cache, "ICON_GL_SKILL_002.png"), file);
        Assert.Equal(1, server.Requests);
        Assert.Equal(SkillIconCache.BaseUrl + "ICON_GL_SKILL_002.png", server.LastUrl);
    }

    [Theory]
    [InlineData(11_020_000, "ICON_GL_SKILL_002")]
    [InlineData(17_720_000, "ICON_CL_SKILL_Passive_002")]
    [InlineData(30_000_000, null)]
    [InlineData(30_000_010, "Icon_Item_Usable_Godstone_WP_r_001")]
    [InlineData(30_001_200, "Icon_Item_Usable_Godstone_WP_r_014")]
    [InlineData(5_000, null)]
    public void Skill_icon_names_follow_the_game_data(int skillCode, string? expected)
    {
        using var icons = new SkillIconCache(Data, Cache, enabled: false);

        Assert.Equal(expected, icons.IconName(skillCode));
    }

    [Fact]
    public void Skill_icon_overrides_come_from_the_data_folder()
    {
        Directory.CreateDirectory(Data);
        File.WriteAllText(Path.Combine(Data, "skill_icons.json"), """{"1102": "ICON_GL_SPECIAL"}""");
        using var icons = new SkillIconCache(Data, Cache, enabled: false);

        Assert.Equal("ICON_GL_SPECIAL", icons.IconName(11_020_000));
    }

    [Fact]
    public async Task A_missing_skill_icon_is_not_asked_again_in_the_session()
    {
        var server = new FakeServer(HttpStatusCode.NotFound, "text/html");
        using var icons = new SkillIconCache(Data, Cache, enabled: true, server);

        icons.FileOf(11_020_000);
        await WaitIdle(() => icons.Busy);
        icons.FileOf(11_020_000);
        await WaitIdle(() => icons.Busy);

        Assert.Equal(1, server.Requests);
    }

    [Fact]
    public void Disabled_downloads_never_reach_the_network()
    {
        var server = new FakeServer(HttpStatusCode.OK, "image/png");
        using var icons = new SkillIconCache(Data, Cache, enabled: false, server);

        Assert.Null(icons.FileOf(11_020_000));
        Assert.Equal(0, server.Requests);
    }

    [Fact]
    public async Task A_portrait_the_site_lacks_is_remembered_across_restarts()
    {
        var server = new FakeServer(HttpStatusCode.Forbidden, "text/html");
        using (var first = new BossPortraitCache(Data, Cache, enabled: true, server))
        {
            first.FileOf(2_300_218);
            await WaitIdle(() => first.Busy);
        }
        using var second = new BossPortraitCache(Data, Cache, enabled: true, server);

        Assert.Null(second.FileOf(2_300_218));
        Assert.Equal(1, server.Requests);
    }

    [Fact]
    public async Task A_portrait_that_is_not_webp_is_a_miss()
    {
        var server = new FakeServer(HttpStatusCode.OK, "text/html");
        using var portraits = new BossPortraitCache(Data, Cache, enabled: true, server);

        portraits.FileOf(2_300_218);
        await WaitIdle(() => portraits.Busy);

        Assert.False(File.Exists(Path.Combine(Cache, "2300218.webp")));
        Assert.Null(portraits.FileOf(2_300_218));
    }

    [Fact]
    public async Task Mapped_dungeon_bosses_use_their_model_name()
    {
        Directory.CreateDirectory(Data);
        File.WriteAllText(Path.Combine(Data, "boss_portraits.json"), """{"portraits": {"2400101": "dungeon/nuakum"}}""");
        var server = new FakeServer(HttpStatusCode.OK, "image/webp");
        using var portraits = new BossPortraitCache(Data, Cache, enabled: true, server);

        portraits.FileOf(bossCode: 2_400_101, targetCode: 99);
        await WaitIdle(() => portraits.Busy);

        Assert.Equal(BossPortraitCache.BaseUrl + "dungeon/nuakum.webp", server.LastUrl);
        Assert.Equal(Path.Combine(Cache, "dungeon_nuakum.webp"), portraits.FileOf(2_400_101));
    }

    private static async Task WaitIdle(Func<bool> busy)
    {
        for (var i = 0; i < 200 && busy(); i++) await Task.Delay(10);
        Assert.False(busy());
    }

    private sealed class FakeServer(HttpStatusCode status, string mediaType) : HttpMessageHandler
    {
        private int _requests;

        public int Requests => _requests;

        public string? LastUrl { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref _requests);
            LastUrl = request.RequestUri!.ToString();
            var content = new ByteArrayContent([1, 2, 3]);
            content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            return Task.FromResult(new HttpResponseMessage(status) { Content = content });
        }
    }
}
