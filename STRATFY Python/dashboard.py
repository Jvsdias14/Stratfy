import streamlit as st
import pandas as pd
import requests
import matplotlib.pyplot as plt
import matplotlib.colors as mcolors

st.set_page_config(layout="wide")

# Captura parâmetros da URL
query = st.query_params

dashboard_id = str(query.get("dashboardId", "")).strip()

if dashboard_id:
    url = f"http://localhost:5211/api/dashboarddata/{dashboard_id}"
    response = requests.get(url)

    if response.status_code == 200:
        data = response.json()

        df = pd.DataFrame(data["movimentacoes"])
        df.columns = df.columns.str.lower()

        st.title(f"Dashboard: {data['dashboard']['descricao']}")

        # ====== Filtros na barra lateral ======
        st.sidebar.header("Filtros")

        # Filtro por data
        if "data" in df.columns:
            datas = pd.to_datetime(df["data"])
            data_inicio = st.sidebar.date_input("Data inicial", datas.min())
            data_fim = st.sidebar.date_input("Data final", datas.max())
            df = df[(datas >= pd.to_datetime(data_inicio)) & (datas <= pd.to_datetime(data_fim))]

        # Filtro por categoria
        if "categoria" in df.columns:
            categorias = df["categoria"].dropna().unique().tolist()
            categorias_selecionadas = st.sidebar.multiselect("Categorias", categorias, default=categorias)
            df = df[df["categoria"].isin(categorias_selecionadas)]

        # ====== Cartões (métricas) ======
        if data["cartoes"]:
            colunas_cards = st.columns(len(data["cartoes"]))
            for i, card in enumerate(data["cartoes"]):
                campo = card["campo"].lower()
                tipo_agregacao = card["tipoAgregacao"].lower()

                if campo in df.columns:
                    if tipo_agregacao == "soma":
                        valor = df[campo].sum()
                    elif tipo_agregacao == "media":
                        valor = df[campo].mean()
                    elif tipo_agregacao == "max":
                        valor = df[campo].max()
                    elif tipo_agregacao == "min":
                        valor = df[campo].min()
                    elif tipo_agregacao == "contagem":
                        valor = df[campo].nunique()
                    else:
                        valor = "Agregação inválida"
                    colunas_cards[i].metric(label=card["nome"], value=valor)
                else:
                    colunas_cards[i].warning(f"Campo inválido: {campo}")

        st.markdown("---")

        # ====== Gráficos ======
        st.subheader("Gráficos")
        graficos = data["graficos"]
        num_por_linha = 2

        for i in range(0, len(graficos), num_por_linha):
            colunas = st.columns(num_por_linha)

            for j, graf in enumerate(graficos[i:i + num_por_linha]):
                with colunas[j]:
                    campo1 = graf["campo1"].lower()
                    campo2 = graf["campo2"].lower()
                    tipo = graf["tipo"].lower()
                    cor = graf.get("cor", "blue").lower()

                    if campo1 in df.columns and campo2 in df.columns:
                        st.subheader(graf["titulo"])

                        if tipo == "barra":
                            agrupado = df.groupby(campo1)[campo2].sum()
                            st.bar_chart(agrupado, color=cor)

                        elif tipo == "linha":
                            agrupado = df.groupby(campo1)[campo2].sum()
                            st.line_chart(agrupado, color=cor)

                        elif tipo == "pizza":
                            dados = df.groupby(campo1)[campo2].sum().abs()

                            # Gerar uma paleta de tons da cor base
                            base_color = mcolors.to_rgb(cor)
                            n = len(dados)
                            palette = [mcolors.to_hex((min(1, base_color[0] + i/n), 
                                                       min(1, base_color[1] + i/n), 
                                                       min(1, base_color[2] + i/n))) for i in range(n)]

                            fig, ax = plt.subplots()
                            ax.pie(dados, labels=dados.index, autopct='%1.1f%%', startangle=90, colors=palette)
                            ax.axis('equal')
                            st.pyplot(fig)

                    else:
                        st.warning(f"Campos inválidos no gráfico: {campo1}, {campo2}")

    else:
        st.error("Dashboard não encontrada.")
else:
    st.warning("Nenhum ID de dashboard foi informado na URL.")
