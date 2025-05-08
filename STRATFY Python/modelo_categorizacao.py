import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.naive_bayes import MultinomialNB
from sklearn.pipeline import Pipeline
import pickle

def treinar_modelo(dados_treinamento):
    """Treina o modelo de categorização."""
    df_treinamento = pd.DataFrame(dados_treinamento)
    X_treino = df_treinamento['descricao']
    y_treino = df_treinamento['categoria']

    modelo = Pipeline([
        ('tfidf', TfidfVectorizer(ngram_range=(1, 2))),
        ('clf', MultinomialNB())
    ])
    modelo.fit(X_treino, y_treino)
    return modelo

def salvar_modelo(modelo, nome_arquivo='modelo_categorizacao.pkl'):
    """Salva o modelo treinado em um arquivo."""
    with open(nome_arquivo, 'wb') as arquivo:
        pickle.dump(modelo, arquivo)

def carregar_modelo(nome_arquivo='modelo_categorizacao.pkl'):
    """Carrega o modelo treinado de um arquivo."""
    try:
        with open(nome_arquivo, 'rb') as arquivo:
            return pickle.load(arquivo)
    except FileNotFoundError:
        print(f"Erro: Arquivo '{nome_arquivo}' não encontrado.")
        return None

def prever_categoria(modelo, descricao):
    """Prevê a categoria para uma dada descrição."""
    if modelo:
        return modelo.predict([descricao])[0]
    return None


if __name__ == '__main__':
    # Dados de treinamento expandidos e revisados com as categorias solicitadas
    dados_treinamento_exemplo = [
        # Moradia
        {"descricao": "Pagamento do aluguel de maio", "categoria": "Moradia"},
        {"descricao": "Taxa de condomínio referente ao mês", "categoria": "Moradia"},
        {"descricao": "Parcela do financiamento imobiliário", "categoria": "Moradia"},
        {"descricao": "Conta de luz da residência", "categoria": "Moradia"},
        {"descricao": "Reparo no chuveiro", "categoria": "Moradia"},
        {"descricao": "Aluguel apartamento referente a junho", "categoria": "Moradia"},
        {"descricao": "Pagamento taxa condominial julho", "categoria": "Moradia"},
        {"descricao": "Prestação casa própria banco", "categoria": "Moradia"},
        {"descricao": "Conta de gás residencial natura", "categoria": "Moradia"},
        {"descricao": "Pequenos reparos hidráulicos", "categoria": "Moradia"},
        {"descricao": "Compra de material de construção", "categoria": "Moradia"},
        {"descricao": "Serviço de dedetização", "categoria": "Moradia"},

        # Saúde
        {"descricao": "Consulta médica online telemedicina", "categoria": "Saúde"},
        {"descricao": "Remédio para dor de cabeça drogaria", "categoria": "Saúde"},
        {"descricao": "Análise clínica sangue urina", "categoria": "Saúde"},
        {"descricao": "Mensalidade academia ginástica", "categoria": "Saúde"},
        {"descricao": "Sessão de acupuntura", "categoria": "Saúde"},
        {"descricao": "Óculos de grau ótica", "categoria": "Saúde"},
        {"descricao": "Psicólogo consulta individual", "categoria": "Saúde"},
        {"descricao": "Consulta com o clínico geral", "categoria": "Saúde"},
        {"descricao": "Compra de ibuprofeno na farmácia", "categoria": "Saúde"},
        {"descricao": "Exame de sangue no laboratório", "categoria": "Saúde"},
        {"descricao": "Mensalidade do plano odontológico", "categoria": "Saúde"},
        {"descricao": "Sessão de terapia online", "categoria": "Saúde"},

        # Educação
        {"descricao": "Boleto mensalidade escolar ensino médio", "categoria": "Educação"},
        {"descricao": "Compra de canetas cadernos livraria", "categoria": "Educação"},
        {"descricao": "Plataforma de curso de desenvolvimento web", "categoria": "Educação"},
        {"descricao": "Taxa de inscrição vestibular", "categoria": "Educação"},
        {"descricao": "Assinatura revista científica", "categoria": "Educação"},
        {"descricao": "Participação em congresso área", "categoria": "Educação"},
        {"descricao": "Material para curso de pintura", "categoria": "Educação"},
        {"descricao": "Mensalidade da escola do filho", "categoria": "Educação"},
        {"descricao": "Compra de material didático", "categoria": "Educação"},
        {"descricao": "Curso de inglês online", "categoria": "Educação"},
        {"descricao": "Inscrição para o workshop", "categoria": "Educação"},
        {"descricao": "Livro para a faculdade", "categoria": "Educação"},

        # Transporte
        {"descricao": "Gasolina comum posto BR", "categoria": "Transporte"},
        {"descricao": "Bilhete único comum recarga online", "categoria": "Transporte"},
        {"descricao": "Ticket azul área azul", "categoria": "Transporte"},
        {"descricao": "Troca de óleo filtro carro", "categoria": "Transporte"},
        {"descricao": "Uber viagem centro bairro", "categoria": "Transporte"},
        {"descricao": "Aluguel de bicicleta compartilhada", "categoria": "Transporte"},
        {"descricao": "Seguro obrigatório veículo DPVAT", "categoria": "Transporte"},
        {"descricao": "Abastecimento de gasolina no posto", "categoria": "Transporte"},
        {"descricao": "Recarga do bilhete único", "categoria": "Transporte"},
        {"descricao": "Pagamento do estacionamento rotativo", "categoria": "Transporte"},
        {"descricao": "Revisão do carro", "categoria": "Transporte"},
        {"descricao": "Corrida de Uber para o aeroporto", "categoria": "Transporte"},

        # Alimentação
        {"descricao": "Compras semana mercado são vicente", "categoria": "Alimentação"},
        {"descricao": "Jantar romântico restaurante italiano", "categoria": "Alimentação"},
        {"descricao": "Ifood pedido hambúrguer", "categoria": "Alimentação"},
        {"descricao": "Sacolão compra de legumes frescos", "categoria": "Alimentação"},
        {"descricao": "Pão de queijo café expresso", "categoria": "Alimentação"},
        {"descricao": "Cesta básica mensal", "categoria": "Alimentação"},
        {"descricao": "Água de coco barraca praia", "categoria": "Alimentação"},
        {"descricao": "Compra de itens no supermercado", "categoria": "Alimentação"},
        {"descricao": "Jantar com amigos no restaurante", "categoria": "Alimentação"},
        {"descricao": "Pedido de lanche por aplicativo", "categoria": "Alimentação"},
        {"descricao": "Compra de frutas e legumes na feira", "categoria": "Alimentação"},
        {"descricao": "Café da manhã na padaria", "categoria": "Alimentação"},

        # Lazer
        {"descricao": "Entrada para o zoológico", "categoria": "Lazer"},
        {"descricao": "Netflix mensalidade plano", "categoria": "Lazer"},
        {"descricao": "Visita ao aquário", "categoria": "Lazer"},
        {"descricao": "Aluguel de filme online", "categoria": "Lazer"},
        {"descricao": "Show de música ao vivo", "categoria": "Lazer"},
        {"descricao": "Piquenique no parque domingo", "categoria": "Lazer"},
        {"descricao": "Jogo de tabuleiro comprado online", "categoria": "Lazer"},
        {"descricao": "Ingressos para o jogo de futebol", "categoria": "Lazer"},
        {"descricao": "Assinatura mensal do streaming de filmes", "categoria": "Lazer"},
        {"descricao": "Ida ao museu no fim de semana", "categoria": "Lazer"},
        {"descricao": "Passeio de bicicleta no parque", "categoria": "Lazer"},
        {"descricao": "Compra de um novo livro de ficção", "categoria": "Lazer"},

        # Contas
        {"descricao": "Pagamento fatura cartão crédito visa", "categoria": "Contas"},
        {"descricao": "Conta de luz baixa renda", "categoria": "Contas"},
        {"descricao": "Vivo fibra fatura mensal", "categoria": "Contas"},
        {"descricao": "Tim controle fatura digital", "categoria": "Contas"},
        {"descricao": "ISSQN prestação serviço", "categoria": "Contas"},
        {"descricao": "IPTU parcela única", "categoria": "Contas"},
        {"descricao": "Seguro residencial anual", "categoria": "Contas"},
        {"descricao": "Pagamento da fatura do cartão de crédito", "categoria": "Contas"},
        {"descricao": "Conta de água do mês", "categoria": "Contas"},
        {"descricao": "Fatura da internet banda larga", "categoria": "Contas"},
        {"descricao": "Mensalidade do celular", "categoria": "Contas"},
        {"descricao": "Imposto sobre serviços (ISS)", "categoria": "Contas"},

        # Vestuário
        {"descricao": "Blusa de frio loja Renner", "categoria": "Vestuário"},
        {"descricao": "Sapato social sapataria", "categoria": "Vestuário"},
        {"descricao": "Calça jeans promoção", "categoria": "Vestuário"},
        {"descricao": "Cinto de couro acessório", "categoria": "Vestuário"},
        {"descricao": "Vestido festa final de ano", "categoria": "Vestuário"},
        {"descricao": "Tênis para corrida", "categoria": "Vestuário"},
        {"descricao": "Serviço de costureira barra calça", "categoria": "Vestuário"},
        {"descricao": "Compra de uma camisa nova", "categoria": "Vestuário"},
        {"descricao": "Conserto de um sapato", "categoria": "Vestuário"},
        {"descricao": "Compra de calças jeans", "categoria": "Vestuário"},
        {"descricao": "Acessórios de moda", "categoria": "Vestuário"},
        {"descricao": "Lavagem de roupas na lavanderia", "categoria": "Vestuário"},

        # Taxas
        {"descricao": "Taxa manutenção conta corrente", "categoria": "Taxas"},
        {"descricao": "Imposto renda retido na fonte", "categoria": "Taxas"},
        {"descricao": "Licenciamento anual moto", "categoria": "Taxas"},
        {"descricao": "Taxa inscrição concurso público federal", "categoria": "Taxas"},
        {"descricao": "IOF crédito rotativo", "categoria": "Taxas"},
        {"descricao": "Taxa de emissão boleto", "categoria": "Taxas"},
        {"descricao": "Imposto sobre propriedade de veículos automotores", "categoria": "Taxas"},
        {"descricao": "Taxa de serviço bancário", "categoria": "Taxas"},
        {"descricao": "Imposto de Renda Pessoa Física (IRPF)", "categoria": "Taxas"},
        {"descricao": "Taxa de licenciamento do veículo", "categoria": "Taxas"},
        {"descricao": "Taxa de inscrição do concurso", "categoria": "Taxas"},
        {"descricao": "IOF sobre aplicação financeira", "categoria": "Taxas"},

        # Salário
        {"descricao": "Salário líquido mensal empresa XYZ", "categoria": "Salário"},
        {"descricao": "Pagamento funcionários referente a maio", "categoria": "Salário"},
        {"descricao": "Adiantamento quinzenal", "categoria": "Salário"},
        {"descricao": "Participação nos lucros e resultados", "categoria": "Salário"},
        {"descricao": "Décimo terceiro salário primeira parcela", "categoria": "Salário"},
        {"descricao": "Reembolso de despesas trabalhistas", "categoria": "Salário"},
        {"descricao": "Salário família recebido", "categoria": "Salário"},
        {"descricao": "Crédito de salário mensal", "categoria": "Salário"},
        {"descricao": "Pagamento da folha de funcionários", "categoria": "Salário"},
        {"descricao": "Adiantamento salarial", "categoria": "Salário"},
        {"descricao": "Bonificação anual", "categoria": "Salário"},
        {"descricao": "Recebimento de 13º salário", "categoria": "Salário"},

        # Outros
        {"descricao": "TED recebido de pessoa física", "categoria": "Outros"},
        {"descricao": "Pagamento de boleto de cobrança", "categoria": "Outros"},
        {"descricao": "Saque 24 horas", "categoria": "Outros"},
        {"descricao": "Depósito em dinheiro agência bancária", "categoria": "Outros"},
        {"descricao": "Ajuste positivo de saldo", "categoria": "Outros"},
        {"descricao": "Compra de livro técnico de informática", "categoria": "Outros"},
        {"descricao": "Assinatura de revista de culinária", "categoria": "Outros"},
        {"descricao": "Doação para ONG ambiental", "categoria": "Outros"},
        {"descricao": "Reembolso de passagem aérea", "categoria": "Outros"},
        {"descricao": "Tarifa de transferência bancária", "categoria": "Outros"},
        {"descricao": "Transferência bancária recebida", "categoria": "Outros"},
        {"descricao": "Pagamento de boleto diverso", "categoria": "Outros"},
        {"descricao": "Saque em caixa eletrônico", "categoria": "Outros"},
        {"descricao": "Depósito em conta corrente", "categoria": "Outros"},
        {"descricao": "Ajuste de saldo", "categoria": "Outros"},
    ]

    # Treinar o modelo
    modelo_treinado = treinar_modelo(dados_treinamento_exemplo)

    # Salvar o modelo treinado
    salvar_modelo(modelo_treinado)

    print("Modelo de categorização treinado e salvo como 'modelo_categorizacao.pkl'")

    # Carregar o modelo treinado
    modelo_carregado_teste = carregar_modelo()

    # Testar a previsão
    if modelo_carregado_teste:
        descricao_teste = "Abastecimento no posto de gasolina Shell"
        categoria_prevista_teste = prever_categoria(modelo_carregado_teste, descricao_teste)
        print(f"A descrição: '{descricao_teste}' foi categorizada como: '{categoria_prevista_teste}'")

        descricao_teste_2 = "TED recebido de João Silva - pagamento"
        categoria_prevista_teste_2 = prever_categoria(modelo_carregado_teste, descricao_teste_2)
        print(f"A descrição: '{descricao_teste_2}' foi categorizada como: '{categoria_prevista_teste_2}'")

        descricao_teste_3 = "Compra de um livro de receita"
        categoria_prevista_teste_3 = prever_categoria(modelo_carregado_teste, descricao_teste_3)
        print(f"A descrição: '{descricao_teste_3}' foi categorizada como: '{categoria_prevista_teste_3}'")