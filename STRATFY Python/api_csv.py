from flask import Flask, request, jsonify
import pandas as pd
from flask_cors import CORS

app = Flask(__name__)
CORS(app)

# Mapeamento de sinônimos por campo
sinonimos = {
    "descricao": ["histórico", "detalhes", "description", "descrição", "desc", "hist", "nome"],
    "valor": ["valor", "valor final", "valor lançamento", "valor (r$)", "amount", "val"],
    "tipo": ["tipo", "tipo operação", "tipo de lançamento", "forma pagamento", "transaction_type"],
    "data": ["data", "data operação", "data lançamento", "time", "data movimentação"]
}

def detectar_colunas(df):
    sinonimos = {
        "descricao": ["descrição", "descricao", "histórico", "detalhes", "description"],
        "valor": ["valor", "valor final", "valor lançamento", "amount", "valor (r$)"],
        "tipo": ["tipo", "forma pagamento", "tipo de lançamento", "tipo operação", "transaction_type"],
        "data": ["data", "data operação", "data lançamento", "time"]
    }

    df.columns = df.columns.str.strip().str.lower()
    colunas_detectadas = {}

    for chave, sinonimos in sinonimos.items():
        match = df.columns[df.columns.str.contains('|'.join(sinonimos), case=False, na=False)]
        if not match.empty:
            colunas_detectadas[chave] = match[0]

    return colunas_detectadas


@app.route('/api/uploadcsv', methods=['POST'])
def processar_csv():
    if 'file' not in request.files:
        return jsonify({'erro': 'Arquivo ausente'}), 400

    file = request.files['file']
    if file.filename == '':
        return jsonify({'erro': 'Arquivo vazio'}), 400

    try:
        # Leitura com fallback de encoding
        try:
            df = pd.read_csv(file, sep=None, engine='python', encoding='utf-8')
        except UnicodeDecodeError:
            file.stream.seek(0)
            df = pd.read_csv(file, sep=None, engine='python', encoding='latin1')

        df.columns = df.columns.str.strip()  # limpa espaços
        mapeadas = detectar_colunas(df)

        campos_necessarios = ["descricao", "valor", "tipo", "data"]
        if not all(campo in mapeadas for campo in campos_necessarios):
            return jsonify({'erro': f'Colunas obrigatórias ausentes. Detectadas: {mapeadas}'}), 400

        df = df.rename(columns={mapeadas[campo]: campo for campo in campos_necessarios})

        try:
            df["data"] = pd.to_datetime(df["data"]).dt.date
        except:
            df["data"] = pd.to_datetime(df["data"], errors='coerce').dt.date

        df = df.dropna(subset=["descricao", "valor", "tipo", "data"])

        movimentacoes = []
        for _, row in df.iterrows():
            movimentacoes.append({
                "Descricao": str(row["descricao"]),
                "Valor": float(row["valor"]),
                "Tipo": str(row["tipo"]),
                "DataMovimentacao": row["data"].isoformat()
            })

        return jsonify(movimentacoes)

    except Exception as e:
        import traceback
        traceback.print_exc()
        return jsonify({'erro': str(e)}), 500



if __name__ == "__main__":
    app.run(port=8000, debug=True, use_reloader=False)
