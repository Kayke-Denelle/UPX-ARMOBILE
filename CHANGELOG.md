# Changelog

Histórico das principais versões do **Museu AR**.

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
