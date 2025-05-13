import pandas as pd
import matplotlib.pyplot as plt
from matplotlib.ticker import PercentFormatter


df = pd.read_excel('results.xlsx', sheet_name='VR Template-Interactobot')
df['Coverage'] = df['Coverage'].astype(float)*100

# Define the time points you want to label and their new labels
time_points = [60, 120, 180, 240, 300]
labels = [1, 2, 3, 4, 5]

plt.figure(figsize=(8, 5))
plt.plot(df["Time"], df["Coverage"], marker='o', linestyle='-', color='b')
plt.ylim(0, 100)
plt.gca().yaxis.set_major_formatter(PercentFormatter())

# Set custom x-ticks and labels
plt.xticks(time_points, labels)

plt.grid(axis='y', linestyle='--', alpha=0.6)
ax = plt.gca()
ax.spines['top'].set_visible(False)
# ax.spines['bottom'].set_visible(False)
ax.spines['right'].set_visible(False)
# ax.spines['left'].set_visible(False)

plt.tight_layout()
plt.show()
