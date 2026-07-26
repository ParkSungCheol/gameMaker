package com.example.test;

import android.content.Intent;
import android.graphics.Color;
import android.graphics.PorterDuff;
import android.graphics.drawable.Drawable;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Message;
import android.view.Gravity;
import android.view.View;
import android.view.animation.Animation;
import android.view.animation.AnimationUtils;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.annotation.Nullable;
import androidx.annotation.RequiresApi;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.content.ContextCompat;
import androidx.lifecycle.ViewModelProvider;
import androidx.vectordrawable.graphics.drawable.Animatable2Compat;

import com.bumptech.glide.Glide;
import com.bumptech.glide.RequestManager;
import com.bumptech.glide.load.DataSource;
import com.bumptech.glide.load.engine.GlideException;
import com.bumptech.glide.load.resource.gif.GifDrawable;
import com.bumptech.glide.request.RequestListener;
import com.bumptech.glide.request.target.Target;

public class UpgradeActivity extends AppCompatActivity {
    // MVVM
    private MonsterViewModel monsterViewModel;
    private RequestManager glide = null;

    // fade_out animation
    Animation animFadeOut = null;
    TextView fade = null;

    void runnableByName(String name) {
        class OneShotTask implements Runnable {
            String name;
            OneShotTask(String name) { this.name = name; }
            @RequiresApi(api = Build.VERSION_CODES.N)
            public void run() {
                if(name == null) {
                    monsterViewModel.setHaveMonsters();
                }
                else {
                    monsterViewModel.setMoney(name);
                }
            }
        }
        Thread t = new Thread(new OneShotTask(name));
        t.start();
    }

    Handler handler = null;

    void runnableByMonster(String name) {
        class OneShotTask implements Runnable {
            String name;
            OneShotTask(String name) { this.name = name; }
            @RequiresApi(api = Build.VERSION_CODES.N)
            public void run() {
                Message msg = Message.obtain(handler, 0, 0, 0);
                Bundle data = new Bundle();
                try {
                    monsterViewModel.upgrade(name, "A");
                } catch (customedException e) {
                    data.putString("message", e.getMessage());
                    msg.setData(data);
                    handler.sendMessage(msg);
                    return;
                }
            }
        }
        Thread t = new Thread(new OneShotTask(name));
        t.start();
    }

    @RequiresApi(api = Build.VERSION_CODES.N)
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        glide = Glide.with(this);
        overridePendingTransition(R.anim.fade_in, R.anim.fade_out);
        // activity_main xml을 view로 사용
        setContentView(R.layout.upgrade);
        // MVVM
        monsterViewModel = new ViewModelProvider(this, new MonsterViewModelFactory(this.getApplication())).get(MonsterViewModel.class);
        // fade_out animation
        animFadeOut = AnimationUtils.loadAnimation(this, R.anim.fade_out);
        // 나타났다 사라지는 textView
        fade = findViewById(R.id.fade4);
        // main thread에게 작업지시
        handler = new Handler(){
            public void handleMessage(Message msg){
                // 원래 하려던 동작 (UI변경 작업 등)
                alert(msg.getData().getString("message"));
            }
        };

        monsterViewModel.getHaveMonsters().observe(this, haveMonsters -> {
            LinearLayout imageLayout = findViewById(R.id.imageLayout);
            LinearLayout textLayout = findViewById(R.id.textLayout);
            LinearLayout.LayoutParams imageParams = new LinearLayout.LayoutParams(400, 400);
            imageParams.setMargins(50, 0, 50, 0);
            LinearLayout.LayoutParams textParams = new LinearLayout.LayoutParams(400, 150);
            textParams.setMargins(50, 50, 50, 0);
            for(Monster monster : haveMonsters) {
                // name에 해당하는 이미지 동적생성
                ImageView newImg1 = new ImageView(this);
                newImg1.setScaleType(ImageView.ScaleType.FIT_CENTER);
                newImg1.setLayoutParams(imageParams);
                newImg1.setTag(monster.getName());
                setViewResource(monster.getName(), "move", newImg1);
                newImg1.setOnClickListener(new View.OnClickListener() {
                    @Override
                    public void onClick(View v) {
                        ImageView iv = (ImageView) v;
                        setViewResource(iv.getTag().toString(), "attack", iv);
                    }
                });
                imageLayout.addView(newImg1);
                Button newImg2 = new Button(this);
                newImg2.setLayoutParams(textParams);
                newImg2.setTextColor(Color.WHITE);
                newImg2.setTag(monster.getName());
                newImg2.setPadding(0,10,0,10);
                newImg2.setGravity(Gravity.CENTER);
                newImg2.getBackground().setColorFilter(ContextCompat.getColor(this, R.color.black), PorterDuff.Mode.MULTIPLY);
                if(monster.isCastle()) {
                    newImg2.setText("LEVEL : " + monster.getUpgradeCount() + "\n [ " + (50 * (monster.getUpgradeCount() + 1)) + " ] ");
                }
                else {
                    newImg2.setText("LEVEL : " + monster.getUpgradeCount() + "\n [ " + (monster.getCost() * (monster.getUpgradeCount() + 1)) + " ] ");
                }
                newImg2.setOnClickListener(new View.OnClickListener() {
                    @Override
                    public void onClick(View v) {
                        runnableByMonster(v.getTag().toString());
                    }
                });
                textLayout.addView(newImg2);
            }
        });

        monsterViewModel.getMoney().observe(this, money -> {
            TextView text = findViewById(R.id.moneyText2);
            text.setText("MONEY [ " + money + " ]");
        });

        monsterViewModel.getUpdatedMonster().observe(this, updatedMonster -> {
            LinearLayout buttons = findViewById(R.id.textLayout);
            for(int i = 0; i < buttons.getChildCount(); i++) {
                Button subView = (Button)buttons.getChildAt(i);
                if(subView.getTag().equals(updatedMonster.getName())) {
                    if(updatedMonster.isCastle()) {
                        subView.setText("LEVEL : " + updatedMonster.getUpgradeCount() + "\n [ " + (50 * (updatedMonster.getUpgradeCount() + 1)) + " ] ");
                    }
                    else{
                        subView.setText("LEVEL : " + updatedMonster.getUpgradeCount() + "\n [ " + (updatedMonster.getCost() * (updatedMonster.getUpgradeCount() + 1)) + " ] ");
                    }
                    break;
                }
            }
        });

        runnableByName(null);
        // money 화면에 show
        runnableByName("A");

        ImageView homeButton = findViewById(R.id.homeButton3);
        homeButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                // TODO convert activity to MainActivity
                Intent intent = new Intent(UpgradeActivity.this, MainActivity.class);
                startActivity(intent);
                finish();
            }
        });
    }

    public void setViewResource(String name, String action, ImageView newImg) {
        int checkExistence = getResources().getIdentifier(name + action, "drawable", getPackageName());
        if ( checkExistence != 0 ) {  // the resource exists
            if(action.equals("attack")) {
                glide.asGif()
                        .load(getResources().getIdentifier(name + action , "drawable", getApplicationContext().getPackageName()))
                        .listener(new RequestListener<GifDrawable>() {
                            @Override
                            public boolean onLoadFailed(@Nullable @org.jetbrains.annotations.Nullable GlideException e, Object model, Target<GifDrawable> target, boolean isFirstResource) {
                                return false;
                            }

                            @Override
                            public boolean onResourceReady(GifDrawable resource, Object model, Target<GifDrawable> target, DataSource dataSource, boolean isFirstResource) {
                                resource.setLoopCount(1);
                                resource.registerAnimationCallback(new Animatable2Compat.AnimationCallback() {
                                    @Override
                                    public void onAnimationEnd(Drawable drawable) {
                                        //do whatever after specified number of loops complete
                                        glide.load(getResources().getIdentifier(name + "move" , "drawable", getApplicationContext().getPackageName())).into(newImg);
                                    }
                                });
                                return false;
                            }
                        })
                        .into(newImg);
            }
            else {
                glide.load(getResources().getIdentifier(name + action , "drawable", getApplicationContext().getPackageName())).into(newImg);
            }
        }
        else {  // checkExistence == 0  // the resource does NOT exist
            newImg.setImageResource(getResources().getIdentifier(name, "drawable", getApplicationContext().getPackageName()));
        }
    }

    // 안내문구 출력(나왔다가 사라지는 안내문구)
    public void alert(String message) {
        fade.bringToFront();
        fade.setText(message);
        fade.setVisibility(View.VISIBLE);
        fade.startAnimation(animFadeOut);
    }
}

