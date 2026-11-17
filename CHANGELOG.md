# Changelog

Histórico das principais versões do **Museu AR**.

## [1.3.0] — 2026-11-16 (versionCode 4)

### Removido
- **Modo Acervo**: o app funciona só pelo QR code das placas. Saíram a barra Visita/Acervo, o menu do acervo e o botão "Ver em 3D" das peças relacionadas (que abria a peça nesse modo).
- Atalho **M** (trocar modo) no simulador de PC.

### Alterado
- Painel de informações rente à borda inferior (não há mais barra de modos), deixando mais tela para a peça.
- O código do Modo Acervo continua no projeto: para religar, `CollectionModeEnabled = true` em `MuseuARSceneBuilder.cs` e **Museu AR › Reconstruir interface**.

## [1.2.0] — 2026-11-16 (versionCode 3)

APK no repositório: [`apk/MuseuAR.apk`](apk/MuseuAR.apk) (44 MB). Os QR codes apontam para <https://github.com/Kayke-Denelle/UPX-ARMOBILE/releases/latest>.

### Adicionado
- **Botões com ícones** desenhados no projeto (`UiIcons.cs`): informações, pontos, resetar, fechar, tocar/pausar (troca sozinho), ver em 3D, visita e acervo; seta (›) em cada item do acervo.
- **QR codes apontando para o download do app** (`.../releases/latest#<id da peça>`).
- **Folhas A4 para impressão** em `docs/placas/` (nome da peça, instruções e placa com 15 cm) e menu **Museu AR › 5. Exportar placas para impressão (A4)**.
- **4 peças novas** no acervo (agora são 6): vaso de barro, urna com frisos florais, fruteira de pedra e par de facas — cada uma com 4 pontos de interesse, ficha, placa com QR code e miniatura.
- Peças relacionadas: vaso de barro ↔ vaso de gargalo longo; urna ↔ fruteira.
- Pasta `ModelosOriginais/` com os arquivos baixados (`.zip`, `.rar`, `.blend`).

### Alterado
- **Interface do app na mesma paleta fosca da tela inicial**: painéis em azul-grafite, botões grafite, destaques em areia (categoria, progresso, áudio, modo selecionado), textos em branco quente e cinza suave.
- Barra de modos: o modo selecionado fica em areia com texto escuro; o outro se mistura à barra.
- Pontos de interesse em 3D: areia (não vistos) e verde-sálvia (visitados), no lugar do amarelo e do verde vivos.
- Miniaturas do acervo refeitas com fundo grafite.

### Corrigido
- Marcadores dos pontos aparecendo rosa (erro do shader `URP/Unlit` salvo no cache por falta de memória).

- **Tela inicial redesenhada**, com visual fosco e discreto: fundo em degradê suave azul-grafite (sem brilhos), emblema "AR" com contorno fino, passos de uso em cartões grafite com números em tons foscos (verde-sálvia, rosa antigo, areia) e botão **Iniciar** em cor sólida areia, sem brilho.
- Miniaturas das peças retiradas da tela inicial.
- Fundo da tela inicial gerado pelo editor (`StartScreenArt.cs`, onde fica a paleta de cores) em `Assets/MuseuAR/Data/UI/`.

## [1.1.0] — 2026-10-28 (versionCode 2)

### Documentação
- Diagramas do README com termos em português.
- Hardware de teste: Xiaomi POCO X7 Pro. Roteiro de testes no celular (seção 22.2) preenchido com **dados simulados**, a substituir pelos resultados reais.

### Adicionado
- **Tela inicial**: nome do museu, miniaturas das peças, 3 passos de uso e botão **Iniciar**. A RA só liga depois do toque (no PC, Enter também inicia).

### Removido
- Peças de exemplo (ânfora e lamparina): modelos, fichas, placas e o menu "Museu AR › 2. Criar peças de exemplo". O acervo agora tem só o vaso e o documento antigo.
- Ligação do vaso com a ânfora (o ponto "Bojo" virou um detalhe da própria peça).

### Alterado
- Nome do museu no catálogo: "Museu AR".

### Documentação
- README reorganizado no modelo da disciplina UPX — Realidade Aumentada.
- Guia de conteúdo em `docs/documents/guia-de-conteudo.md` e capturas em `docs/images/`.

## 2026-10-20 — Placas com QR code e build Android (`dbaabcc`)

### Adicionado
- Gerador de QR code no editor (`QrCode.cs`), sem dependências externas.
- Menu **Museu AR › 3. Gerar placas com QR code**: uma placa por peça, com QR no centro e moldura ilustrada única.
- Campo **Qr Content** no catálogo para definir o que o QR abre (`{id}` = id da peça).
- Menu **Museu AR › 4. Gerar APK (Android)**.

### Alterado
- App "Museu AR" (`com.upx.museuar`), Android 10+ (API 29), ARM64/IL2CPP, apenas a cena do museu no build.
- Textos do app passam a falar em "QR code" em vez de "placa AR".

### Verificado
- 4/4 placas lidas corretamente pelo leitor ZXing; links de até 138 caracteres testados.
- APK gerado sem erros (43 MB).

## 2026-10-19 — Interface compacta e novas peças (`e6cd06f`)

### Adicionado
- Peças **Vaso de gargalo longo** e **Documento antigo** em `Assets/Peças`, com material, pontos de interesse, ficha, placa e miniatura.
- Miniaturas no menu do Modo Acervo.
- Menu **Museu AR › Reconstruir interface (na cena aberta)**.

### Alterado
- Interface refeita: painéis de tamanho fixo presos às bordas, cantos arredondados, fontes menores; painéis encolhem conforme o conteúdo.
- Rótulos 3D menores; a seta começa na borda do texto.
- Formatos `.igs`, `.iges` e `.glb` adicionados ao Git LFS.

### Corrigido
- Arquivos binários restaurados do Git LFS (estavam apenas como ponteiros no disco).

## 2026-09-30 — Protótipo (`7c5d48e`)

### Adicionado
- Modo Visita (Image Tracking) e Modo Acervo (Plane Detection).
- Pontos de interesse com marcador, seta e rótulo 3D; painel com texto e áudio; peças correlacionadas.
- Peças de exemplo (ânfora e lamparina) e simulador para testar no PC.

## Check-in inicial (`c913b38`)

- Projeto criado a partir do template **AR Mobile** da Unity 6.
