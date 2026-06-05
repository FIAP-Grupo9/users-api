namespace FCG.Users.Api.Application;

public static class Messages
{
    public const string EmailFormatoInvalido = "Email com formato inválido.";
    public const string SenhaInsegura = "A senha deve ter no mínimo 8 caracteres, com letras, números e caracteres especiais.";
    public const string EmailJaExiste = "Já existe um usuário cadastrado com esse e-mail.";
    public const string EmailOuSenhaInvalidos = "E-mail ou senha inválidos.";
    public const string UsuarioNaoEncontrado = "Usuário não encontrado.";
    public const string UsuarioSemPermissao = "Sem permissão para acessar este recurso.";
    public const string TokenInvalidoOuExpirado = "Token inválido ou expirado.";
    public const string TokenRecuperacaoEnviado = "Token de recuperação gerado. Em produção seria enviado por e-mail.";
}
