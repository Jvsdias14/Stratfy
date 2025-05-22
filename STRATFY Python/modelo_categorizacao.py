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

    # Treinar o modelo
    dados_treinamento_exemplo = pd.read_json('TreinoML.json')
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