import sys
from statistics import mean, median, variance, stdev
import matplotlib.pyplot as plt

def draw(x, y):
  
  plt.rcParams['font.family'] = 'Kozuka Gothic Pro'

  fig, ax = plt.subplots()
  ax.scatter(x, y)
  plt.title('アイマストドンユーザ分布(' + date + ' ' + time + '現在)')
  plt.xlabel('今日のトゥート数')
  plt.ylabel('人数')
  plt.text(0.6, 0.8, '$n=%d,\\ sum=%d,\\ max=%d,$\n$med=%d,\\ \\mu=%.2f,\\ \\sigma=%.2f$' % (n, total, maximum, med, mu, sdev), transform=ax.transAxes)
  plt.text(0.5, 0.95, '今日1トゥート以上のアカウントが対象。', transform=ax.transAxes)
  plt.text(0.5, 0.9, '公開トゥートのみ対象。', transform=ax.transAxes)
  plt.show()

if __name__ == "__main__":
  try:
    argv = sys.argv
    argv.pop(0)
    x = eval(argv.pop(0))
	y = eval(argv.pop(0))
    draw(x, y)
  except:
    import traceback
    traceback.print_exc()
    input('>> ')
  