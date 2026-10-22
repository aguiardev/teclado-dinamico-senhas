# Teclado dinâmico de senhas

Console app em C# que demonstra como funciona o teclado dinâmico de senhas usado em aplicativos de banco. O foco é o algoritmo de embaralhamento e de comparação dos hashes.

Criado para uma live da comunidade [Aguiar Dev](https://tiagoaguiardev.com.br/comunidade). Baseado no artigo [How bank passwords work](https://felipperegazio.com/posts/how-bank-passwords-works/).

## Como funciona

1. **Cadastro (servidor):** a senha é guardada só como hash PBKDF2 com salt.
2. **Sessão (servidor):** os dígitos de 0 a 9 são embaralhados em 5 grupos de 2. Cada grupo ganha um token (HMAC-SHA256 com uma chave aleatória da sessão) e o servidor guarda o mapa `token → grupo`.
3. **Digitação (cliente):** o usuário escolhe grupos. O que vai para o servidor são apenas os tokens, nunca os dígitos.
4. **Validação (servidor):** os tokens são revertidos pelo mapa da sessão, o servidor gera todas as combinações possíveis e compara o hash de cada uma com o hash cadastrado.
5. **Replay:** a sessão é descartada, e os mesmos tokens enviados de novo são rejeitados.

## Como executar

Requer o .NET 10 SDK.

```bash
dotnet run --project src/TecladoDinamicoSenhas
```

Use as setas para navegar, `ENTER` para adicionar o grupo, `BACKSPACE` para apagar o último e `>> CONFIRMAR <<` para enviar. A senha cadastrada na demo é `1234`.

## Estrutura

| Arquivo | Conteúdo |
|---|---|
| `Program.cs` | O roteiro da demonstração, passo a passo |
| `Algoritmo.cs` | Embaralhamento, tokens, reversão, combinações e comparação |
| `Interface.cs` | Menu com Spectre.Console e laço de teclas |

O código é procedural, sem classes de domínio. O `partial class Program` é só o contêiner dos métodos.

## Limites da técnica

Cada grupo carrega 2 dígitos, então o servidor testa várias combinações. A técnica protege o canal e o cliente contra interceptação e keyloggers, mas não substitui rate limiting nem outras camadas de segurança.
