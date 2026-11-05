# Pixel Jumper

Jogo de plataforma 2D simples feito em Unity.

## Requisitos
- Unity **2022.3.62f3**
- Template 2D, Built-In Render Pipeline
- C#

## Controles
| Ação | Tecla |
|------|-------|
| Mover | A / D ou setas |
| Pular | Space (ou W / seta para cima) |
| Reiniciar | R |

## O jogo
Plataforma 2D em pixel art. Chegue à bandeira dourada no fim da fase.

- Colete as moedas (o HUD mostra quantas faltam e o tempo).
- Desvie dos espinhos e não caia nos buracos: você volta ao último checkpoint (bandeira verde).
- Pulo variável (segure Space para pular mais alto), coyote time e buffer de pulo.
- Animações de idle, corrida, pulo e queda, com poeira e squash and stretch.
- Fundo com parallax, câmera suave e efeitos sonoros gerados por código.

## Como abrir
1. Clone o repositório.
2. No Unity Hub, clique em *Add* e selecione a pasta do projeto.
3. Abra a cena `Assets/Scenes/Main.unity` e dê Play.

## Estrutura
```
Assets/
├── Art/        sprites pixel art (gerados por código)
├── Editor/     geradores de arte e da cena
├── Prefabs/    Player
├── Scenes/     Main.unity
└── Scripts/    PlayerController, PlayerAnimator, CameraFollow,
                ParallaxLayer, GameManager, Coin, Hazard, Checkpoint, Goal
```

## Recriar a cena e a arte
No Unity, use o menu **Pixel Jumper > Criar Cena Main**. Ele redesenha os sprites em
`Assets/Art` e remonta a fase, o fundo, o jogador, a câmera e o HUD.
