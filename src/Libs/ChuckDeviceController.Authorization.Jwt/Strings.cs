namespace ChuckDeviceController.Authorization.Jwt;

internal class Strings
{
    //public const string DefaultContentType = "application/grpc";
    public const string DefaultGrpcServiceIdentifier = "Grpc";
    public const string DefaultInternalServiceIdentifier = "InternalService";

    public const string AuthorizationHeader = "Authorization";
    public const string ClaimTypeNameRole = "role";
    //public const string IgnoreJwtValidationHeader = "IgnoreJwtValidation";

    public const string DefaultApiEndpoint = "http://127.0.0.1:8881";
    public const string JwtEndpoint = "/api/jwt/generate?identifier=Grpc";
}