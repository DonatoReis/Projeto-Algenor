Componentes Principais do Jogo
NavigationAgentController.cs
Função: Controla o agente principal do jogo, integrando todos os sistemas necessários para seu funcionamento, incluindo movimento, observação, recompensas e objetivos.

Integração: Une os sistemas de movimento (AgentMovement), recompensas (AgentRewardSystem), observação (AgentObservationSystem) e objetivos (AgentObjectiveSystem).
Aprendizado por Reforço: Utiliza o algoritmo PPO (Proximal Policy Optimization) para treinar o agente com base nas recompensas e penalidades recebidas.
Curriculum Learning: Ajusta os parâmetros do ambiente durante o treinamento para aumentar gradualmente a dificuldade e ensinar novas habilidades ao agente.
AgentMovement.cs
Função: Define os comportamentos de movimento do agente, incluindo andar, girar e pular, além de verificar se o agente está no chão.

Movimentação: Controla a velocidade de movimento (moveSpeed), força do pulo (jumpForce) e velocidade de rotação (rotateSpeed).
Verificação de Solo: Utiliza Physics.SphereCast para determinar se o agente está tocando o chão, permitindo pular apenas quando está no solo.
Configurações Adaptativas: Permite ajustar se o agente pode se movimentar ou pular, e define a altura máxima do pulo, suportando o curriculum learning.
AgentObservationSystem.cs
Função: Gerencia as observações do agente, coletando dados do ambiente para serem utilizados na tomada de decisão.

Percepção com Raycasts: Utiliza 41 raycasts (7 baixos, 17 médios e 17 altos) com um campo de visão de 70 graus para detectar objetos ao redor.
Tipos de Objetos Detectados: O agente identifica diferentes tipos de objetos, como chão, parede, obstáculos e plataformas.
Empilhamento de Observações: Mantém um histórico de 6 observações para fornecer memória de curto prazo, ajudando o agente a lembrar de informações recentes do ambiente.
AgentRewardSystem.cs
Função: Define a função de recompensa do agente, aplicando recompensas e penalidades com base em suas ações e desempenho.

Recompensas:
Exploração: Incentiva o agente a explorar o ambiente.
Alcançar Objetivos: Recompensa ao atingir metas específicas, como chegar a um ponto designado.
Movimento Positivo: Recompensa por se mover na direção correta.
Penalidades:
Inatividade: Penaliza se o agente ficar parado por muito tempo.
Colisões: Penaliza ao colidir com paredes ou cair do mapa.
Tempo: Pequenas penalidades contínuas para incentivar ações mais rápidas.
AgentObjectiveSystem.cs
Função: Gerencia os objetivos do agente dentro do jogo, incluindo a lógica para alcançar metas e monitorar o progresso.

Objetivos Específicos: Define metas como alcançar uma porta ou explorar uma sala dentro de um limite de tempo.
Controle de Tempo: Monitora o tempo gasto em cada tarefa, reiniciando o agente se o tempo esgotar.
Interação com o Ambiente: Interage com outros componentes, como o SpawnAreaManager, para reposicionar o agente quando necessário.
AgentObservationSystem.cs
Função: Coleta e processa as observações do agente, fornecendo informações detalhadas sobre o ambiente para a rede neural.

Raycasts Detalhados: Realiza raycasts em diferentes ângulos e alturas para mapear o ambiente.
Histórico de Observações: Mantém um registro das observações anteriores para fornecer contexto temporal.
Dados Coletados: Inclui posição, velocidade, se está no chão, distâncias para objetivos e direções relevantes.
Door.cs
Função: Controla o comportamento da porta no jogo, permitindo que ela abra e feche com base nas interações do agente.

Movimentação Suave: Utiliza corrotinas para mover a porta de forma gradual entre as posições aberta e fechada.
Integração com Plataformas: Reage às ações do agente nas plataformas de pressão, abrindo ou fechando conforme necessário.
TargetPlatform.cs
Função: Representa as plataformas de pressão que o agente deve ativar para interagir com outros elementos, como portas.

Interação com o Agente: Muda de cor e posição ao ser ativada pelo agente, fornecendo feedback visual.
Temporização: Retorna ao estado original após um período sem contato do agente.
Áudio: Reproduz sons ao ser ativada ou desativada, melhorando a imersão.
SpawnAreaManager.cs
Função: Gerencia a área de reaparecimento do agente, definindo posições aleatórias dentro de uma área específica.

Posicionamento Aleatório: Calcula posições dentro da área de spawn para reposicionar o agente de forma variada.
Verificação de Solo: Garante que o agente seja colocado em uma posição válida no chão, evitando erros de posicionamento.
Visualização: Desenha gizmos no editor para facilitar a configuração da área de spawn.
Treinamento do Agente
Método de Aprendizado
Algoritmo: O agente é treinado utilizando o Aprendizado por Reforço Profundo com o algoritmo PPO (Proximal Policy Optimization).
Ambiente Simultâneo: Para acelerar o treinamento, múltiplas instâncias do agente são treinadas simultaneamente em cópias do ambiente.
Curriculum Learning: O treinamento é estruturado em lições, aumentando gradualmente a dificuldade e introduzindo novas habilidades ao agente.
Observações do Agente
Percepção Ambiental: O agente utiliza raycasts para detectar o ambiente, coletando informações sobre:
Presença de Objetos: Se há um objeto em determinada direção.
Distância: Quão longe está o objeto detectado.
Tipo de Objeto: Identifica se é chão, parede, obstáculo ou plataforma.
Empilhamento de Observações: Com um histórico de 6 observações, o agente possui memória de curto prazo, permitindo decisões mais informadas.
Arquitetura da Rede Neural
Entrada: Recebe as observações coletadas pelo AgentObservationSystem, totalizando um número significativo de inputs devido ao número de raycasts e empilhamento.
Camadas Ocultas: Possui 4 camadas ocultas com 512 unidades cada, permitindo a aprendizagem de representações complexas do ambiente.
Saída: Gera ações contínuas (movimento para frente/trás, rotação) e discretas (pulo), permitindo controle completo do agente.
Ações do Agente
Movimentação: Pode se mover para frente ou para trás com velocidade variável.
Rotação: Pode girar para a esquerda ou direita.
Pulo: Pode executar um pulo se as condições permitirem (por exemplo, se estiver no chão e se o pulo for permitido pelo curriculum).
Função de Recompensa
Recompensas Positivas:
Alcançar Objetivos: Recompensa ao atingir metas definidas pelo AgentObjectiveSystem.
Exploração: Pequenas recompensas por explorar o ambiente, incentivando a descoberta.
Movimento Direcionado: Recompensas por se mover na direção correta dos objetivos.
Penalidades:
Queda: Penalização significativa ao cair do mapa.
Colisões: Penalizações menores ao colidir com paredes ou obstáculos.
Inatividade: Penaliza se o agente permanecer inativo por muito tempo.
Penalização por Tempo: Penalidades contínuas para incentivar a conclusão rápida das tarefas.
Objetivos do Agente e Expectativas
Navegação Eficiente: O agente deve aprender a navegar pelo ambiente de forma eficaz, evitando obstáculos e alcançando os objetivos definidos.
Interação com Objetos: Deve utilizar plataformas de pressão e outros elementos interativos para progredir no ambiente.
Gestão do Tempo: Precisa completar as tarefas dentro dos limites de tempo estabelecidos, aprendendo a priorizar ações importantes.
Adaptação: Com o curriculum learning, o agente deve se adaptar a novas habilidades e desafios progressivamente mais difíceis.
Progresso do Jogo
Fase Inicial
Posicionamento Aleatório: O agente é colocado em uma posição aleatória dentro da área de spawn.
Exploração: Deve começar a explorar o ambiente, coletando informações através dos raycasts.
Interação com Plataformas
Ativação de Plataformas: Ao encontrar uma plataforma de pressão, o agente deve ativá-la para desencadear eventos, como abrir portas.
Feedback Visual e Sonoro: As plataformas fornecem indicações claras de seu estado atual, auxiliando o agente a entender as consequências de suas ações.
Avanço de Fase
Checkpoints: Alcançar certos pontos no ambiente pode levar o agente a uma nova fase ou reiniciar desafios com maior dificuldade.
Aumento de Dificuldade: Novos obstáculos, layouts ou limitações podem ser introduzidos para desafiar o agente e incentivar o aprendizado contínuo.
Objetivo Final
Completar o Ambiente: O agente deve aplicar todo o aprendizado adquirido para superar todos os desafios apresentados e atingir o objetivo final definido pelo jogo.
Interação com o Jogador
Elementos Visuais: O jogo apresenta informações como tempo restante e número de gerações (tentativas) para monitorar o progresso do agente.
Ambiente Responsivo: Os elementos do jogo reagem às ações do agente, criando um ambiente dinâmico e imersivo.
Parâmetros e Configurações Importantes
Arquivo config.yaml
trainer_type: ppo - Define o algoritmo de treinamento utilizado.
hyperparameters: Configurações específicas do algoritmo PPO, como batch_size, buffer_size, learning_rate, entre outros.
network_settings:
hidden_units: 512 - Número de unidades em cada camada oculta.
num_layers: 4 - Número de camadas ocultas na rede neural.
reward_signals: Define os tipos de sinais de recompensa utilizados, como extrinsic e curiosity.
environment_parameters: Parâmetros do curriculum learning, ajustando aspectos como maxJumpHeight, allowMovement, allowJump, exploreRoom e hasObjective ao longo das lições.
Conclusão
O agente foi projetado para aprender a navegar e interagir em um ambiente complexo, utilizando técnicas avançadas de aprendizado por reforço. Com uma arquitetura robusta e configurações cuidadosamente ajustadas, o agente é capaz de evoluir suas habilidades e adaptar-se a novos desafios, culminando em um comportamento eficiente e inteligente dentro do jogo.