import pandas as pd
import matplotlib.pyplot as plt
from matplotlib.ticker import PercentFormatter


scenes = [
    "VR Template",
    "XRI Assets",
    "EscapeProto"
]

target_scene = scenes[2]
# Read both sheets
df_interactobot = pd.read_excel('results.xlsx', sheet_name=f'{target_scene}-Interactobot')
df_random = pd.read_excel('results.xlsx', sheet_name=f'{target_scene}-Interactobot_rand')

# Convert coverage to percentage
df_interactobot['Coverage'] = df_interactobot['Coverage'].astype(float)*100
df_random['Coverage'] = df_random['Coverage'].astype(float)*100

# Define the time points you want to label and their new labels
time_points = [60, 120, 180, 240, 300]
labels = [r'\textbf{1}', r'\textbf{2}', r'\textbf{3}', r'\textbf{4}', r'\textbf{5}']

plt.figure(figsize=(10, 6))
# Plot both lines with different colors and add labels for legend

plt.rcParams.update({
    "text.usetex": True,        # Enable LaTeX
    "font.family": "serif",     # Use serif font (optional, LaTeX default is serif)
})
plt.plot(df_interactobot["Time"], df_interactobot["Coverage"],
         linestyle='-', color='slateblue', linewidth=5, label=r'\textsc{\textbf{InteractoBot}}')
plt.plot(df_random["Time"], df_random["Coverage"],
         linestyle='--', color='darkorange', linewidth=5, label=r'\textsc{\textbf{InteractoBot}}$_{rand}$')

plt.ylim(0, 100)
plt.gca().yaxis.set_major_formatter(PercentFormatter())

# Create custom formatter for y-axis to make ticks bold


def bold_formatter(x, pos):
    return r'\textbf{' + f'{int(x)}' + '\%' + r'}'


plt.gca().yaxis.set_major_formatter(plt.FuncFormatter(bold_formatter))

plt.xticks(time_points, labels, fontsize=28)
plt.yticks(fontsize=28)

# Add legend with larger font size
plt.legend(fontsize=28, loc='upper center', bbox_to_anchor=(0.5, -0.15), ncol=2)

plt.grid(axis='y', linestyle='--', alpha=0.6)
ax = plt.gca()
ax.spines['top'].set_visible(False)
# ax.spines['bottom'].set_visible(False)
ax.spines['right'].set_visible(False)
# ax.spines['left'].set_visible(False)

plt.tight_layout()
# Save the plot with 300 DPI
plt.savefig(f'RQ3_{target_scene}.png', dpi=300, bbox_inches='tight')
plt.show()
