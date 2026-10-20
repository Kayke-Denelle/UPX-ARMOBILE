# Museu AR — Realidade Aumentada para museus

App mobile (Android/iOS) feito em Unity + AR Foundation que permite ao visitante **ver em 3D peças que não podem ser manuseadas**, girar, ampliar e descobrir **curiosidades ancoradas na própria peça**, apresentadas com setas, textos curtos, áudio e ligações com outras peças do acervo.

---

## 1. A ideia

### O problema
- Peças frágeis ficam atrás do vidro: o visitante não vê o verso, o fundo nem os detalhes.
- Grande parte do acervo fica na **reserva técnica** e nunca é exposta.
- As etiquetas têm pouco espaço, e o conteúdo extra (contexto, técnica, curiosidades) fica de fora.
- A relação entre peças (mesmo período, mesmo uso, mesma técnica) raramente é mostrada.

### A solução
O visitante usa o próprio celular. O app tem dois modos:

| Modo | Como funciona | Para quê |
|---|---|---|
| **Visita** | Ao lado de cada vitrine há uma **placa com QR code**. O visitante aponta a câmera do app e a peça aparece em 3D sobre a placa. | Ver de todos os ângulos a peça que está atrás do vidro. |
| **Acervo** | O visitante escolhe uma peça num catálogo e a posiciona numa mesa ou no chão. | Ver peças da reserva técnica e peças correlacionadas. |

Em ambos os modos, a peça 3D traz **pontos de interesse (hotspots)**:

```
        Bocal ───────►●        ● = marcador pulsante no ponto exato da peça
                     ╔═╗       ► = seta que liga o rótulo ao ponto
    Alças ──────────►●║ ║       Rótulo 3D = sempre virado para o visitante
                   ( ● )◄──── Corpo
                    \_/
                     ●◄────── Base pontuda
```

Tocar num hotspot abre um painel com:
- **título + categoria** (Curiosidade, Detalhe, Técnica, História, Correlação);
- **texto** curto;
- **áudio** narrado (opcional), com play/pausa e barra de progresso;
- **"Veja também"**: botão que abre a **peça correlacionada** em 3D.

O hotspot visitado fica verde, e o cabeçalho mostra o progresso ("Curiosidades descobertas: 2/4"). É um pequeno incentivo para explorar a peça inteira.

### Jornada do visitante
1. Abre o app → **Modo Visita** → "Aponte a câmera para o QR code".
2. Aponta para o QR da ânfora → a ânfora aparece em pé, virada para ele.
3. Arrasta o dedo para girar e faz pinça para ampliar. Vê o fundo da peça.
4. Toca em "Corpo" → lê/ouve que as ânforas levavam azeite → toca em **"Ver em 3D: Lamparina"**.
5. O app muda para o **Modo Acervo**, e o visitante toca na mesa → a lamparina (que está na reserva) aparece.

---

## 2. Como rodar (primeira vez)

Requisitos: Unity **6000.6** (versão do projeto), AR Foundation 6.x, ARCore/ARKit (já instalados).

1. Abra a cena **`Assets/MuseuAR/Scenes/MuseuAR.unity`** (já é a 1ª cena do Build).
   - Se ela não existir: menu **Museu AR › 1. Criar cena do museu**.
2. Se as peças de exemplo não existirem: menu **Museu AR › 2. Criar peças de exemplo**.
3. **Teste no PC:** veja a seção abaixo.
4. **Teste no celular:** veja "Instalando no celular" abaixo.

### Instalando no celular (Android)

1. Menu **Museu AR › 4. Gerar APK (Android)**. O arquivo sai em **`Builds/MuseuAR.apk`** (o primeiro build leva de 10 a 20 min).
2. Passe o APK para o celular (cabo USB, Google Drive, WhatsApp…) e toque nele para instalar. O Android pede para permitir "instalar apps desconhecidos": permita para o app que abriu o arquivo.
   - Alternativa com cabo: ative a *Depuração USB* no celular e use *File › Build Profiles › Build And Run*.
3. O celular precisa ser **compatível com ARCore** (Google Play Services for AR). A lista oficial está em <https://developers.google.com/ar/devices>. Na primeira abertura, o app pode pedir para instalar/atualizar esse serviço.
4. **Imprima as placas com QR code** (uma por peça) com **15 cm de largura**, em papel fosco:

| Peça | Placa |
|---|---|
| Ânfora romana | `Assets/MuseuAR/Exemplos/Placas/placa-anfora.png` |
| Lamparina a óleo | `Assets/MuseuAR/Exemplos/Placas/placa-lamparina.png` |
| Vaso de gargalo longo | `Assets/Peças/Vaso/placa-vaso.png` |
| Documento antigo | `Assets/Peças/Ancient_Paper/placa-papel.png` |

5. Abra o app, deixe em **Visita** e aponte para uma placa a uns 30–50 cm. A peça aparece sobre a placa: **arraste o dedo para girar**, faça **pinça para ampliar** e toque nos pontos.

> iOS: o projeto já tem o ARKit, mas o build para iPhone precisa de um Mac com Xcode.

### As placas com QR code

Cada placa tem um **QR code no centro** e uma **moldura ilustrada** em volta:

- **A moldura** é o que o app rastreia em AR. O ARCore precisa de muitos cantos e contraste; um QR sozinho é rastreado mal (a peça treme ou demora a aparecer).
- **O QR** abre um link na câmera comum do celular, útil para quem ainda não tem o app. Por enquanto ele contém só um identificador (`MUSEUAR:<id da peça>`).
- **Para trocar o link:** em `Assets/MuseuAR/Data/CatalogoDoMuseu`, mude o campo **Qr Content** (ex.: `https://meumuseu.org/app` ou `https://meumuseu.org/pecas/{id}`, onde `{id}` vira o id de cada peça). Depois use **Museu AR › 3. Gerar placas com QR code**, gere o APK de novo e reimprima as placas.
- A moldura de cada peça é sempre a mesma; ao gerar de novo, só o QR muda.

### Testando no PC (sem celular)

O projeto usa o **XR Simulation**: no Play, o Unity finge ser um celular dentro de uma sala 3D. O componente `DesktopSimulator` (em `MuseuAR_Systems`) adiciona atalhos para fazer no PC tudo o que se faz no celular. Ele se desliga sozinho no celular.

**Antes de dar Play:** na aba **Game**, escolha uma resolução em retrato (ex.: *1080x1920*; use o "+" do menu de resolução para criar uma, se não houver). O app foi desenhado para celular em pé.

**Depois de dar Play, clique uma vez dentro da Game view** para ela receber o teclado.

| No celular | No PC |
|---|---|
| Andar com o celular | Segure o **botão direito** do mouse + **W A S D** (Q desce, E sobe, Shift corre). Mova o mouse para olhar. |
| Apontar para o QR code (Modo Visita) | Tecla **1** = QR da peça 1 do catálogo, **2** = peça 2, … |
| Tocar (ponto, botão, mesa) | **Clique** com o botão esquerdo |
| Arrastar o dedo (girar a peça) | **Arrastar** com o botão esquerdo, ou setas **← →** |
| Pinça (zoom) | **Roda do mouse**, ou **+ / −** |
| Tocar na mesa (Modo Acervo) | Clique no chão à sua frente. Sem superfície detectada, o app usa uma *mesa virtual* 60 cm abaixo da câmera. |
| Botões da tela | Clique, ou atalhos: **I** sobre a peça, **Esc** fechar painel, **P** pontos, **R** resetar, **M** trocar modo |
| — | **H** mostra/esconde o painel de ajuda |

**Roteiro de teste rápido:**
1. Play → clique na Game view → aperte **1**. A ânfora aparece com os pontos amarelos.
2. Clique num ponto (ex.: *Corpo*). O painel abre, o ponto fica verde e o progresso vira 1/4.
3. Clique em **"Ver em 3D: Lamparina"**. O app vai para o Modo Acervo. Clique no chão à frente para posicionar a lamparina.
4. Arraste para girar e use a roda do mouse para dar zoom.

É normal o console mostrar *"Could not find reference image…"*: a sala simulada tem imagens próprias que não estão na nossa biblioteca.

---

## 3. Como cadastrar uma peça real

Nenhuma linha de código é necessária: tudo é feito por *assets*.

Cada peça fica numa pasta própria dentro de **`Assets/Peças/`**, com o modelo, o material, o prefab, a ficha e a placa:

```
Assets/Peças/
├── Vaso/            vase_01.obj · Vaso_CeramicaVidrada.mat · Vaso.prefab · Vaso.asset · placa-vaso.png
└── Ancient_Paper/   paper.3DS + texturas · PapelAntigo.mat · PapelAntigo.prefab · PapelAntigo.asset · placa-papel.png
```

1. **Modelo 3D:** importe a peça em **`.fbx` ou `.obj`** (fotogrametria com Polycam/RealityScan, Sketchfab com licença, ou modelado no Blender).
   - **Não funcionam direto:** `.max` (precisa do 3ds Max instalado; exporte como `.fbx`), `.igs`/`.step` (CAD; converta no Rhino/FreeCAD) e `.glb` (precisa do pacote *glTFast*).
   - Crie um **prefab** com a origem na **base da peça** e o eixo **+Z** virado para "a frente".
   - Mantenha o modelo leve: **< 50 mil triângulos** e texturas de até 2K.
2. **Hotspots:** dentro do prefab, crie objetos vazios no ponto que quer destacar e adicione **`HotspotPoint`**:
   - `title` (curto, até ~25 caracteres), `kind`, `description`, `narration` (áudio opcional);
   - `labelOffset`: onde o rótulo flutua, em relação ao ponto. A linha amarela na Scene view mostra a posição;
   - `relatedItem`: peça correlacionada (opcional).
3. **Ficha:** *Create › Museu AR › Peça do Acervo*. Preencha nome, período, origem, material, descrição, `modelPrefab`, `introNarration` e `relatedItems`.
   - Ajuste `initialScale` (modelos em cm → `0.01`), `spawnOffset` e `spawnRotation` se necessário.
4. **Placa com QR code:** arraste a ficha para o catálogo (passo 5) e use **Museu AR › 3. Gerar placas com QR code**. A placa é criada ao lado da ficha e registrada em `Assets/MuseuAR/Data/PlacasAR` com 15 cm.
   - Para usar uma arte própria, adicione a imagem em `PlacasAR`, marque *Specify Size* com o tamanho impresso e dê um **nome igual** ao `referenceImageName` da ficha. Boas placas têm muito contraste, muitos cantos e nenhuma repetição.
5. Arraste a ficha para **`Assets/MuseuAR/Data/CatalogoDoMuseu`**.

Use como referência `Assets/Peças/Vaso` (modelo importado) e `Assets/MuseuAR/Exemplos` (ânfora e lamparina).

> **Textos provisórios:** o vaso e o documento antigo têm período "a definir" e curiosidades gerais sobre cerâmica e papel. Troque pelos dados reais da peça com a equipe do museu.

**Áudio:** grave narrações curtas (30–60 s) em `.mp3`/`.ogg`. Para protótipo, um gerador de voz (TTS) já resolve. Nas configurações de importação, use *Load Type: Streaming* ou *Compressed In Memory*.

---

## 4. Arquitetura

```
Assets/MuseuAR/
├── Scenes/MuseuAR.unity           cena do app
├── Data/                          catálogo, estilo dos hotspots, biblioteca de placas
├── Exemplos/                      peças de exemplo (modelos, placas, fichas)
└── Scripts/
    ├── Data/        MuseumItemData (ficha da peça) · MuseumCatalog (lista de peças)
    ├── AR/          ImageTrackingSpawner (Modo Visita) · FreePlacementController (Modo Acervo)
    ├── Exhibit/     ExhibitController (peça na cena: zoom, giro, foco)
    │                HotspotPoint (dados do ponto) · HotspotView (marcador + seta + rótulo 3D)
    │                HotspotStyle (cores/tamanhos) · Billboard
    ├── Interaction/ TouchInputRouter (toque/mouse, arrastar, pinça/roda) · DesktopSimulator (modo PC)
    ├── Narration/   NarrationPlayer (audioguia)
    ├── UI/          InfoPanelUI (cabeçalho + painel + correlacionados) · CatalogMenuUI · SafeAreaFitter
    ├── Core/        AppModeController (troca Visita/Acervo, dicas na tela)
    └── Editor/      MuseuARSceneBuilder (monta a cena) · MuseuARSampleContent (peças de exemplo)
```

Fluxo principal:

```
Placa reconhecida ─► ImageTrackingSpawner ─┐
                                            ├─► ExhibitController.Spawn(peça) ─► HotspotView p/ cada HotspotPoint
Toque na mesa ─────► FreePlacementController┘                │
                                                             ▼
TouchInputRouter ── toque no hotspot ──► InfoPanelUI ──► NarrationPlayer (áudio)
                 └─ arrastar/pinça ───► ExhibitController.Active (girar/zoom)
InfoPanelUI ── "Ver em 3D" ──► AppModeController.OpenInCollection(peça relacionada)
```

Decisões importantes:
- **Conteúdo separado do código** (ScriptableObjects): a equipe do museu cadastra peças sem programar.
- **Marcadores e rótulos com tamanho fixo no mundo real**, mesmo quando o visitante amplia a peça.
- **Peça "em pé" e virada para o visitante** no Modo Visita, mesmo com a placa colada na parede.
- **A peça continua visível quando a placa sai do enquadramento** (`hideWhenLimited = false`), para o visitante poder chegar perto e girar.

---

## 5. Roadmap sugerido

**Etapa 1 — Protótipo (feito)**
- [x] Modo Visita (placa com QR code) e Modo Acervo (posicionar na mesa)
- [x] Hotspots com seta, rótulo 3D, texto e áudio
- [x] Girar/ampliar, progresso de curiosidades, correlacionados clicáveis
- [x] Cena e peças de exemplo geradas pelo menu *Museu AR*

**Etapa 2 — Conteúdo real**
- [ ] Escolher 3–5 peças de um museu parceiro e fazer fotogrametria
- [ ] Escrever os textos com a equipe educativa e gravar os áudios
- [ ] Criar placas com a identidade visual do museu (moldura própria em volta do QR)
- [ ] Testar em celulares intermediários (desempenho e iluminação real das salas)

**Etapa 3 — Experiência**
- [ ] Tela inicial e onboarding (como usar a placa, pedir fone de ouvido)
- [ ] Legendas para os áudios e opção de tamanho de fonte (acessibilidade)
- [ ] Idiomas (PT/EN/ES): trocar strings por *Localization package*
- [ ] Animações: "explodir" partes da peça ou mostrar a peça restaurada (antes/depois)
- [ ] Quiz ou "caça ao tesouro" com base nas curiosidades descobertas

**Etapa 4 — Escala**
- [ ] Baixar peças por **Addressables** (o app não precisa carregar todo o acervo)
- [ ] Painel web para o museu editar textos sem gerar uma nova versão do app
- [ ] Métricas anônimas: peças e curiosidades mais vistas

---

## 6. Dicas e problemas comuns

| Problema | Solução |
|---|---|
| A placa não é reconhecida | Imprima com 15 cm de largura, em papel fosco (sem reflexo), com a moldura inteira. Ilumine bem a placa e aproxime o celular a 30–50 cm. Se você gerou placas novas, gere o APK de novo: as placas vão dentro do app. |
| O Android não deixa instalar o APK | Permita "instalar apps desconhecidos" para o app que abriu o arquivo (Arquivos, Drive, WhatsApp…). |
| O app abre mas a câmera fica preta ou ele fecha | O celular precisa ser compatível com ARCore e ter o *Google Play Services for AR* atualizado (Play Store). Aceite a permissão de câmera. |
| A peça aparece enorme ou minúscula | Ajuste `initialScale` na ficha (modelos exportados em cm → `0.01`). |
| A peça aparece deitada | Ajuste `spawnRotation` na ficha (ex.: `-90, 0, 0`). |
| Os rótulos se sobrepõem | Afaste os `labelOffset` dos hotspots, para lados diferentes da peça. |
| Não consigo me mexer no PC | Clique dentro da Game view e **segure o botão direito** enquanto usa WASD. |
| Teclas não respondem no PC | A Game view precisa estar em foco: clique nela uma vez. |
| Menus no topo da Unity | **Museu AR › 1. Criar cena do museu**, **2. Criar peças de exemplo**, **3. Gerar placas com QR code**, **4. Gerar APK (Android)**, **Montar cena base (na cena aberta)**, **Reconstruir interface (na cena aberta)**. |
| Quero mudar o layout da interface | O layout fica em `MuseuARSceneBuilder.BuildUI` (tamanhos em unidades de uma tela 1080x1920). Depois de editar, use **Museu AR › Reconstruir interface** e salve a cena. |
| A interface parece enorme no PC | A Game view está deitada ou baixa. Use uma resolução em retrato (ex.: 1080x1920). |
