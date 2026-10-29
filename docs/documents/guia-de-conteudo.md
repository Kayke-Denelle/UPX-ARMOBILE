# Guia de conteúdo — Museu AR

Guia prático para cadastrar peças, gerar as placas com QR code, testar no PC e gerar o APK.
Nenhuma etapa exige programação: tudo é feito por *assets* e pelos menus **Museu AR** no topo da Unity.

---

## 1. Menus do projeto

| Menu | O que faz |
|---|---|
| **Museu AR › 1. Criar cena do museu** | Cria `Assets/MuseuAR/Scenes/MuseuAR.unity` a partir do template AR (só é necessário se a cena não existir). |
| **Museu AR › 3. Gerar placas com QR code** | Gera uma placa por peça do catálogo e registra na biblioteca de imagens AR. |
| **Museu AR › 4. Gerar APK (Android)** | Gera `Builds/MuseuAR.apk`. |
| **Museu AR › Montar cena base (na cena aberta)** | Monta sistemas e interface numa cena que já tenha AR Session + XR Origin. |
| **Museu AR › Reconstruir interface (na cena aberta)** | Recria a interface depois de mudar o layout em `MuseuARSceneBuilder.BuildUI`. |

---

## 2. Como cadastrar uma peça

Cada peça fica numa pasta própria dentro de **`Assets/Peças/`**:

```text
Assets/Peças/
├── Vaso/            vase_01.obj · Vaso_CeramicaVidrada.mat · Vaso.prefab · Vaso.asset · placa-vaso.png
└── Ancient_Paper/   paper.3DS + texturas · PapelAntigo.mat · PapelAntigo.prefab · PapelAntigo.asset · placa-papel.png
```

1. **Modelo 3D:** importe a peça em **`.fbx` ou `.obj`** (fotogrametria com Polycam/RealityScan, Sketchfab com licença ou Blender).
   - **Não funcionam direto:** `.max` (precisa do 3ds Max; exporte como `.fbx`), `.igs`/`.step` (CAD; converta no Rhino/FreeCAD) e `.glb` (precisa do pacote *glTFast*).
   - Crie um **prefab** com a origem na **base da peça** e o eixo **+Z** virado para a frente.
   - Mantenha o modelo leve: **menos de 50 mil triângulos** e texturas de até 2K.
2. **Hotspots:** dentro do prefab, crie objetos vazios nos pontos de interesse e adicione o componente **`HotspotPoint`**:
   - `title` (até ~25 caracteres), `kind` (Curiosidade, Detalhe, Técnica, História, Correlação), `description`, `narration` (áudio opcional);
   - `labelOffset`: posição do rótulo em relação ao ponto (a linha amarela na Scene view mostra onde fica);
   - `relatedItem`: peça correlacionada (opcional; aparece como "Veja também").
3. **Ficha:** *Create › Museu AR › Peça do Acervo*. Preencha nome, período, origem, material, descrição, `modelPrefab`, `introNarration` e `relatedItems`.
   - Ajuste `initialScale` (modelos em cm → `0.01`), `spawnOffset` e `spawnRotation` se necessário.
4. **Catálogo:** arraste a ficha para **`Assets/MuseuAR/Data/CatalogoDoMuseu`**.
5. **Placa:** use **Museu AR › 3. Gerar placas com QR code**. A placa é criada ao lado da ficha.
6. **Miniatura (opcional):** um `Sprite` no campo `thumbnail` da ficha aparece no menu do Modo Acervo.

**Áudio:** narrações curtas (30–60 s) em `.mp3`/`.ogg`. Para protótipo, um gerador de voz (TTS) resolve. Na importação, use *Load Type: Streaming* ou *Compressed In Memory*.

---

## 3. Placas com QR code

![Placa com QR code do vaso](../../Assets/Pe%C3%A7as/Vaso/placa-vaso.png)

Cada placa tem um **QR code no centro** e uma **moldura ilustrada**:

- **A moldura** é o que o app rastreia (Image Tracking). O ARCore precisa de muitos cantos e contraste; um QR sozinho tende a ser rastreado de forma instável.
- **O QR** é lido pela câmera comum do celular. Hoje contém um identificador provisório (`MUSEUAR:<id da peça>`).
- **Para trocar o conteúdo do QR:** em `CatalogoDoMuseu`, altere o campo **Qr Content** (ex.: `https://meumuseu.org/app` ou `https://meumuseu.org/pecas/{id}`; `{id}` é trocado pelo id de cada peça). Depois rode o menu 3, gere o APK de novo e reimprima.
- A moldura de cada peça é sempre a mesma; ao gerar de novo, só o QR muda.
- **Impressão:** 15 cm de largura, papel fosco, placa inteira (com a moldura). O tamanho está registrado na biblioteca e define a escala da peça.

| Peça | Arquivo da placa |
|---|---|
| Vaso de gargalo longo | `Assets/Peças/Vaso/placa-vaso.png` |
| Documento antigo | `Assets/Peças/Ancient_Paper/placa-papel.png` |

---

## 4. Testando no PC (XR Simulation)

No Play, o Unity simula um celular dentro de uma sala 3D. O componente `DesktopSimulator` (em `MuseuAR_Systems`) adiciona atalhos de teclado e mouse e se desliga sozinho no celular.

**Antes do Play:** na aba **Game**, use uma resolução em retrato (ex.: 1080x1920). **Depois do Play:** clique uma vez dentro da Game view.

| No celular | No PC |
|---|---|
| Tocar em "Iniciar" (tela inicial) | Clique no botão, ou **Enter** |
| Andar com o celular | Botão **direito** do mouse + **W A S D** (Q desce, E sobe, Shift corre) |
| Apontar para o QR code (Modo Visita) | Tecla **1** = peça 1 do catálogo, **2** = peça 2, … |
| Tocar (ponto, botão, superfície) | **Clique** esquerdo |
| Arrastar o dedo (girar a peça) | **Arrastar** com o botão esquerdo, ou setas **← →** |
| Pinça (zoom) | **Roda do mouse**, ou **+ / −** |
| Tocar na superfície (Modo Acervo) | Clique no chão; sem superfície detectada, o app usa uma *mesa virtual* 60 cm abaixo da câmera |
| Botões da tela | **I** sobre a peça · **Esc** fechar painel · **P** pontos · **R** resetar · **M** trocar modo |
| — | **H** mostra/esconde a ajuda |

O aviso *"Could not find reference image…"* no Console é esperado: vem das imagens da sala simulada.

---

## 5. Gerando e instalando o APK

1. **Museu AR › 4. Gerar APK (Android)** → `Builds/MuseuAR.apk` (o primeiro build levou 24 min; os seguintes são mais rápidos).
2. Copie o APK para o celular e instale (permita "instalar apps desconhecidos").
3. Gere o APK de novo sempre que mudar peças, placas ou o conteúdo do QR: as placas vão dentro do app.
4. `Builds/` não vai para o Git (está no `.gitignore`). Para distribuir, publique o APK na página **Releases** do GitHub.

---

## 6. Antes de cada commit

1. **Ctrl+S** na Unity (salva a cena).
2. **File › Save Project** (salva materiais, prefabs e configurações).
3. Commit e push.

Ao clonar em outro computador, use Git LFS (`git lfs install` antes do `git clone`). Baixar o ZIP pelo GitHub traz só "ponteiros" no lugar das imagens e modelos.
