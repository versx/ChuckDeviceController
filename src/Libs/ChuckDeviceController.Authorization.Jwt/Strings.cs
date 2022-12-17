namespace ChuckDeviceController.Authorization.Jwt
{
    internal class Strings
    {
        //public const string DefaultContentType = "application/grpc";
        public const string DefaultGrpcServiceIdentifier = "Grpc";
        public const string DefaultInternalServiceIdentifier = "InternalService";

        public const string AuthorizationHeader = "Authorization";
        public const string ClaimTypeNameRole = "role";
        //public const string IgnoreJwtValidationHeader = "IgnoreJwtValidation";

        public const string JwtEndpoint = "/api/jwt/generate?identifier=Grpc";
    }
}