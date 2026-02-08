# WebDebugPanel (MAUI Android)

- Parte de cima: WebView carregando uma URL
- Parte de baixo: painel "Dev Tools" simples (console + fetch)
- Debug remoto (DEBUG): usar `chrome://inspect` no PC

## Importante (limitação do Android WebView)
Não existe um "Chrome DevTools completo" embutido no app de forma suportada.
O que dá pra fazer bem:
- Debug remoto via Chrome (recomendado)
- Painel simples dentro do app (este exemplo)

## Como rodar
1) Abra `WebDebugPanel.sln` no Visual Studio
2) Selecione um dispositivo Android e execute em Debug
