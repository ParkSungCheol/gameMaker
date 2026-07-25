package com.example.test;

import android.content.Intent;
import android.os.Build;
import android.os.Bundle;
import android.util.DisplayMetrics;
import android.view.View;
import android.view.ViewGroup;
import android.view.animation.Animation;
import android.view.animation.AnimationUtils;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.annotation.RequiresApi;
import androidx.appcompat.app.AppCompatActivity;
import androidx.constraintlayout.widget.ConstraintLayout.LayoutParams;
import androidx.lifecycle.ViewModelProvider;

public class MapActivity extends AppCompatActivity {

    // fade_out animation
    Animation animFadeOut = null;
    TextView fade = null;

    // MVVM
    private MonsterViewModel monsterViewModel;

    void runnableByName(String name) {
        class OneShotTask implements Runnable {
            String name;
            OneShotTask(String name) { this.name = name; }
            @RequiresApi(api = Build.VERSION_CODES.N)
            public void run() {
                int result = monsterViewModel.setMoney(name);
                if(result == -1) {
                    // 에러발생
                }
            }
        }
        Thread t = new Thread(new OneShotTask(name));
        t.start();
    }

    public void onCreate(Bundle savedInstanceState) {

        super.onCreate(savedInstanceState);

        // activity_main xml을 view로 사용
        setContentView(R.layout.map);

        // MVVM
        monsterViewModel = new ViewModelProvider(this, new MonsterViewModelFactory(this.getApplication())).get(MonsterViewModel.class);

        monsterViewModel.getMoney().observe(this, money -> {
            TextView text = findViewById(R.id.moneyText);
            text.setText("MONEY [ " + money + " ]");
        });

        // 맵 이미지뷰 화면 높이, 너비에 맞추기
        DisplayMetrics displayMetrics = new DisplayMetrics();
        getWindowManager().getDefaultDisplay().getMetrics(displayMetrics);
        int height = this.getWindow().getDecorView().getHeight();
        int width = displayMetrics.widthPixels;
        ImageView img = findViewById(R.id.map);
        LayoutParams params = (LayoutParams) img.getLayoutParams();
        params.width = width;
        params.height = height;
        img.setScaleType(ImageView.ScaleType.FIT_XY);
        img.setLayoutParams(params);

        // fade_out animation
        animFadeOut = AnimationUtils.loadAnimation(this, R.anim.fade_out);
        // 나타났다 사라지는 textView
        fade = findViewById(R.id.fade2);

        ImageView homeButton = findViewById(R.id.homeButton);
        homeButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                // TODO convert activity to MainActivity
                Intent intent = new Intent(MapActivity.this, MainActivity.class);
                startActivity(intent);
                finish();
            }
        });

        // money 화면에 show
        runnableByName("A");

        // 맵 버튼 클릭이벤트 설정
        ViewGroup layout = (ViewGroup)img.getParent();
        for(int i =0; i< layout.getChildCount(); i++){
            View v =layout.getChildAt(i);
            if(v instanceof Button){
                v.setOnClickListener(new View.OnClickListener() {
                    public final void onClick(View it) {
                        Button button = (Button) it;
                        Intent intent = new Intent(MapActivity.this, BattlefieldActivity.class);
                        Bundle b = new Bundle();
                        b.putInt("key", Integer.parseInt(button.getText().toString())); //Your id
                        intent.putExtras(b); //Put your id to your next Intent
                        startActivity(intent);
                        finish();
                    }
                });
            }
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
