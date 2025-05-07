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
    # Dados de treinamento expandidos e revisados
    dados_treinamento_exemplo = [
        # Alimentação
        {"descricao": "Compra no supermercado Pão de Açúcar", "categoria": "ALIMENTAÇÃO"},
        {"descricao": "Almoço no restaurante perto do trabalho", "categoria": "ALIMENTAÇÃO"},
        {"descricao": "Pedido de pizza delivery", "categoria": "ALIMENTAÇÃO"},
        {"descricao": "Feira da fruta e verdura", "categoria": "ALIMENTAÇÃO"},
        {"descricao": "Mercado Extra - compra mensal", "categoria": "ALIMENTAÇÃO"},
        {"descricao": "Padaria - pão e café", "categoria": "ALIMENTAÇÃO"},
        {"descricao": "Assaí Atacadista - compras", "categoria": "ALIMENTAÇÃO"},

        # Contas
        {"descricao": "Pagamento de conta de luz - Elektro", "categoria": "CONTAS"},
        {"descricao": "Conta de água - CEDAE", "categoria": "CONTAS"},
        {"descricao": "Fatura de internet - Claro", "categoria": "CONTAS"},
        {"descricao": "Mensalidade do plano de saúde", "categoria": "CONTAS"},
        {"descricao": "Imposto Predial e Territorial Urbano", "categoria": "CONTAS"},
        {"descricao": "Condomínio do apartamento", "categoria": "CONTAS"},

        # Lazer
        {"descricao": "Ingressos para o cinema", "categoria": "LAZER"},
        {"descricao": "Passeio no parque com a família", "categoria": "LAZER"},
        {"descricao": "Assinatura de streaming de vídeo", "categoria": "LAZER"},
        {"descricao": "Show da banda favorita", "categoria": "LAZER"},
        {"descricao": "Viagem de fim de semana para a praia", "categoria": "LAZER"},
        {"descricao": "Ida ao teatro", "categoria": "LAZER"},

        # Educação
        {"descricao": "Mensalidade da faculdade", "categoria": "EDUCAÇÃO"},
        {"descricao": "Compra de livros técnicos", "categoria": "EDUCAÇÃO"},
        {"descricao": "Curso online de programação", "categoria": "EDUCAÇÃO"},
        {"descricao": "Material escolar dos filhos", "categoria": "EDUCAÇÃO"},
        {"descricao": "Seminário sobre inteligência artificial", "categoria": "EDUCAÇÃO"},

        # Transporte
        {"descricao": "Recarga do cartão de transporte público", "categoria": "TRANSPORTE"},
        {"descricao": "Combustível para o carro", "categoria": "TRANSPORTE"},
        {"descricao": "Estacionamento rotativo", "categoria": "TRANSPORTE"},
        {"descricao": "Manutenção do veículo", "categoria": "TRANSPORTE"},
        {"descricao": "Passagem de ônibus interestadual", "categoria": "TRANSPORTE"},
        {"descricao": "Serviço de carro por aplicativo", "categoria": "TRANSPORTE"},

        # Saúde
        {"descricao": "Consulta médica com especialista", "categoria": "SAÚDE"},
        {"descricao": "Compra de medicamentos na farmácia", "categoria": "SAÚDE"},
        {"descricao": "Exames laboratoriais", "categoria": "SAÚDE"},
        {"descricao": "Mensalidade da academia", "categoria": "SAÚDE"},
        {"descricao": "Sessão de fisioterapia", "categoria": "SAÚDE"},

        # Salário
        {"descricao": "Salário creditado na conta", "categoria": "SALÁRIO"},
        {"descricao": "Pagamento mensal da empresa", "categoria": "SALÁRIO"},

        # Outros (evitando termos de tipo)
        {"descricao": "Rendimento de investimento", "categoria": "INVESTIMENTOS"},
        {"descricao": "Seguro do carro - parcela mensal", "categoria": "SEGUROS"},
        {"descricao": "Empréstimo pessoal - parcela", "categoria": "EMPRÉSTIMOS"},
        {"descricao": "Doação para instituição de caridade", "categoria": "DOAÇÕES"},
        {"descricao": "Reembolso de compra online", "categoria": "REEMBOLSOS"},
        {"descricao": "Tarifa bancária mensal", "categoria": "TARIFAS BANCÁRIAS"},
        {"descricao": "Mesada para os filhos", "categoria": "OUTROS"},
        {"descricao": "Pensão alimentícia", "categoria": "OUTROS"},
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
        descricao_teste = "Abastecimento no posto de gasolina Ipiranga"
        categoria_prevista_teste = prever_categoria(modelo_carregado_teste, descricao_teste)
        print(f"A descrição: '{descricao_teste}' foi categorizada como: '{categoria_prevista_teste}'")

        descricao_teste_2 = "TED recebido de Maria Souza"
        categoria_prevista_teste_2 = prever_categoria(modelo_carregado_teste, descricao_teste_2)
        print(f"A descrição: '{descricao_teste_2}' foi categorizada como: '{categoria_prevista_teste_2}'")