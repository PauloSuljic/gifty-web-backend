namespace Gifty.Tests.DTOs;

public class ShareLinkResponseDto
{
    public string ShareCode { get; set; }

    public ShareLinkResponseDto(string shareCode)
    {
        ShareCode = shareCode;
    }
}